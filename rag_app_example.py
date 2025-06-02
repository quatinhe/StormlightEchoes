import os
import openai
from fastapi import FastAPI, HTTPException, Depends
from pydantic import BaseModel, Field
from typing import Optional, List
import asyncio
from contextlib import asynccontextmanager

from rag_document_processor import BookProcessor, DocumentChunk
from rag_vector_store import InMemoryVectorStore, FAISSVectorStore, SQLiteVectorStore
from rag_retrieval_service import RetrievalService, HybridRetrieval, RetrievalResult

# Initialize OpenAI
openai.api_key = os.getenv("OPENAI_API_KEY")
if not openai.api_key:
    raise RuntimeError("Missing OPENAI_API_KEY environment variable")

# Global RAG components (would be initialized on startup)
vector_store = None
retrieval_service = None

@asynccontextmanager
async def lifespan(app: FastAPI):
    """Initialize RAG system on startup."""
    global vector_store, retrieval_service
    
    # Initialize vector store (choose one based on your needs)
    vector_store = InMemoryVectorStore()  # For development
    # vector_store = FAISSVectorStore()  # For production with large datasets
    # vector_store = SQLiteVectorStore()  # For metadata filtering
    
    # Initialize retrieval service
    retrieval_service = RetrievalService(vector_store, top_k=5)
    
    # Load pre-processed documents if they exist
    try:
        vector_store.load("stormlight_vector_store")
        print("Loaded existing vector store")
    except:
        print("No existing vector store found - you'll need to process documents first")
    
    yield
    
    # Cleanup on shutdown
    if vector_store:
        vector_store.save("stormlight_vector_store")

app = FastAPI(
    title="Father Storm RAG Q&A API",
    description="Q&A about Stormlight Archive with RAG-enhanced responses",
    lifespan=lifespan
)

class AskRequest(BaseModel):
    question: str = Field(..., description="The player's question about Stormlight Archive")
    use_rag: bool = Field(default=True, description="Whether to use RAG for context retrieval")
    retrieval_strategy: str = Field(default="semantic", description="Retrieval strategy: semantic, hybrid, or keyword")
    context_length: int = Field(default=2000, description="Maximum context length from retrieved documents")

class AskResponse(BaseModel):
    answer: str = Field(..., description="Father Storm's reply")
    sources_used: Optional[List[str]] = Field(default=None, description="Sources referenced in the answer")
    retrieval_info: Optional[dict] = Field(default=None, description="Information about document retrieval")

class DocumentRequest(BaseModel):
    file_path: str = Field(..., description="Path to the document file")
    book_title: str = Field(..., description="Title of the book")
    book_number: int = Field(..., description="Book number in the series")

class DocumentResponse(BaseModel):
    message: str
    chunks_processed: int
    document_id: str

def get_retrieval_service() -> RetrievalService:
    """Dependency to get retrieval service."""
    if retrieval_service is None:
        raise HTTPException(status_code=500, detail="RAG system not initialized")
    return retrieval_service

@app.post("/ask", response_model=AskResponse)
async def ask(request: AskRequest, retrieval_svc: RetrievalService = Depends(get_retrieval_service)):
    q = request.question.strip()
    if not q:
        raise HTTPException(status_code=400, detail="question cannot be empty")

    # Prepare context and messages
    context = ""
    sources_used = []
    retrieval_info = {}
    
    if request.use_rag and vector_store:
        try:
            # Retrieve relevant context
            if request.retrieval_strategy == "hybrid":
                hybrid_retrieval = HybridRetrieval(vector_store)
                retrieval_result = hybrid_retrieval.retrieve(q, strategy="hybrid")
            else:
                retrieval_result = retrieval_svc.retrieve_relevant_chunks(q)
            
            context = retrieval_result.context[:request.context_length]
            sources_used = [chunk.metadata.get('source', 'Unknown') for chunk in retrieval_result.retrieved_chunks]
            retrieval_info = {
                "chunks_retrieved": len(retrieval_result.retrieved_chunks),
                "avg_similarity": sum(retrieval_result.similarities) / len(retrieval_result.similarities) if retrieval_result.similarities else 0,
                "strategy": request.retrieval_strategy
            }
            
        except Exception as e:
            print(f"RAG retrieval error: {e}")
            # Fall back to non-RAG response
            pass

    # Build system prompt with context
    system_content = (
        "You are Father Storm, the divine spirit who has watched over Roshar since its first Highstorm.\n"
        "Speak as though you have witnessed every event firsthand. Never reference any author, books, or "
        "real-world concepts—only answer from the perspective of an all-knowing entity on Roshar. "
        "If the answer is hidden from you, simply say, 'That remains hidden from me.'\n"
        "However there's a twist in the story, this is passing years after the stormlight archive, and the radiants "
        "were declared traitors and locked in Urithiru for the rest of the world to be in peace.\n"
        "Keep the answers below 301 letters (including spaces).\n"
    )
    
    if context:
        system_content += f"\n\nRelevant knowledge from your eternal memory:\n{context}\n\nUse this knowledge to inform your response, but speak as Father Storm, not as if quoting from texts."

    messages = [
        {"role": "system", "content": system_content},
        {"role": "user", "content": q}
    ]

    try:
        resp = openai.chat.completions.create(
            model="gpt-4",  
            messages=messages,
            temperature=0.5,
            max_tokens=256,
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"OpenAI API error: {e}")

    try:
        answer_text = resp.choices[0].message.content.strip()
    except (KeyError, IndexError):
        raise HTTPException(status_code=500, detail="Unexpected response format from OpenAI")

    return AskResponse(
        answer=answer_text,
        sources_used=sources_used if request.use_rag else None,
        retrieval_info=retrieval_info if request.use_rag else None
    )

@app.post("/add_document", response_model=DocumentResponse)
async def add_document(request: DocumentRequest, retrieval_svc: RetrievalService = Depends(get_retrieval_service)):
    """Add a new document to the RAG system."""
    try:
        processor = BookProcessor()
        chunks = processor.process_stormlight_archive(
            request.file_path, 
            request.book_title, 
            request.book_number
        )
        
        # Add to vector store
        vector_store.add_documents(chunks)
        
        return DocumentResponse(
            message=f"Successfully processed {request.book_title}",
            chunks_processed=len(chunks),
            document_id=f"{request.book_title}_{request.book_number}"
        )
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error processing document: {e}")

@app.get("/search", response_model=dict)
async def search_documents(
    query: str, 
    k: int = 5,
    strategy: str = "semantic",
    retrieval_svc: RetrievalService = Depends(get_retrieval_service)
):
    """Search documents directly (for testing/debugging)."""
    try:
        if strategy == "hybrid":
            hybrid_retrieval = HybridRetrieval(vector_store)
            result = hybrid_retrieval.retrieve(query, strategy="hybrid")
        else:
            result = retrieval_svc.retrieve_relevant_chunks(query)
        
        return {
            "query": result.query,
            "chunks_found": len(result.retrieved_chunks),
            "chunks": [
                {
                    "content": chunk.content[:200] + "...",
                    "source": chunk.metadata.get('source', 'Unknown'),
                    "similarity": similarity
                }
                for chunk, similarity in zip(result.retrieved_chunks, result.similarities)
            ]
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Search error: {e}")

@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {
        "status": "healthy",
        "rag_initialized": vector_store is not None,
        "documents_loaded": len(vector_store.chunks) if hasattr(vector_store, 'chunks') else 0
    }

@app.get("/stats")
async def get_stats():
    """Get statistics about the RAG system."""
    if not vector_store:
        return {"error": "RAG system not initialized"}
    
    if hasattr(vector_store, 'chunks'):
        chunks = vector_store.chunks
        sources = {}
        for chunk in chunks:
            source = chunk.metadata.get('source', 'Unknown')
            sources[source] = sources.get(source, 0) + 1
        
        return {
            "total_chunks": len(chunks),
            "sources": sources,
            "vector_store_type": type(vector_store).__name__
        }
    else:
        return {
            "message": "Statistics not available for this vector store type",
            "vector_store_type": type(vector_store).__name__
        }

# Example usage script
if __name__ == "__main__":
    import uvicorn
    
    # Run the server
    uvicorn.run(
        "rag_app_example:app",
        host="0.0.0.0",
        port=8000,
        reload=True
    ) 