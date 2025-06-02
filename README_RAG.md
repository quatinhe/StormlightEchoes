# Stormlight Archive RAG System

This is a Retrieval-Augmented Generation (RAG) system implementation for Brandon Sanderson's Stormlight Archive series, designed to enhance the Father Storm Q&A API with accurate, context-aware responses based on the actual book content.

## 🌟 Features

- **Document Processing**: Automatic chunking and embedding of book content
- **Multiple Vector Stores**: In-memory, FAISS, and SQLite options
- **Smart Retrieval**: Semantic search with query expansion and reranking
- **Context-Aware Responses**: Enhanced prompts with relevant book passages
- **FastAPI Integration**: RESTful API with automatic documentation
- **Flexible Architecture**: Modular design for easy customization

## 📁 Project Structure

```
├── rag_document_processor.py    # Document chunking and embedding
├── rag_vector_store.py         # Vector storage implementations
├── rag_retrieval_service.py    # Retrieval and ranking logic
├── rag_app_example.py          # Enhanced FastAPI application
├── setup_rag.py               # Setup and initialization script
├── requirements_rag.txt       # Dependencies
└── README_RAG.md             # This file
```

## 🚀 Quick Start

### 1. Install Dependencies

```bash
pip install -r requirements_rag.txt
```

### 2. Set Environment Variables

```bash
export OPENAI_API_KEY="your-openai-api-key"
```

### 3. Initialize the RAG System

```bash
python setup_rag.py
```

This will:
- Create sample Stormlight Archive content
- Set up the vector store
- Process and embed documents
- Test the retrieval system

### 4. Run the Enhanced API

```bash
python rag_app_example.py
```

The API will be available at `http://localhost:8000` with interactive docs at `http://localhost:8000/docs`

## 🔧 Components Overview

### Document Processor (`rag_document_processor.py`)

Handles text chunking and embedding creation:

```python
from rag_document_processor import BookProcessor

processor = BookProcessor()
chunks = processor.process_stormlight_archive(
    "path/to/book.txt", 
    "The Way of Kings", 
    book_number=1
)
```

**Key Features:**
- Token-based chunking with overlap
- Metadata preservation (book, chapter, series info)
- OpenAI embedding generation
- Optimized for narrative text

### Vector Store (`rag_vector_store.py`)

Three storage options for different use cases:

#### In-Memory Store (Development)
```python
from rag_vector_store import InMemoryVectorStore

store = InMemoryVectorStore()
store.add_documents(chunks)
results = store.search(query_embedding, k=5)
```

#### FAISS Store (Production)
```python
from rag_vector_store import FAISSVectorStore

store = FAISSVectorStore(dimension=1536)
store.add_documents(chunks)
store.save("my_index")  # Persistent storage
```

#### SQLite Store (Metadata Filtering)
```python
from rag_vector_store import SQLiteVectorStore

store = SQLiteVectorStore("books.db")
store.add_documents(chunks)
results = store.search(
    query_embedding, 
    metadata_filter={"series": "The Stormlight Archive"}
)
```

### Retrieval Service (`rag_retrieval_service.py`)

Advanced retrieval with multiple strategies:

```python
from rag_retrieval_service import RetrievalService, HybridRetrieval

# Basic semantic search
service = RetrievalService(vector_store)
result = service.retrieve_relevant_chunks("What are Knights Radiant?")

# Hybrid retrieval with reranking
hybrid = HybridRetrieval(vector_store)
result = hybrid.retrieve("Who is Kaladin?", strategy="hybrid")
```

**Retrieval Strategies:**
- **Semantic**: Pure vector similarity
- **Hybrid**: Two-stage retrieval with reranking
- **Keyword**: BM25-style keyword matching (fallback)

### Enhanced API (`rag_app_example.py`)

Extended FastAPI application with RAG capabilities:

```python
# POST /ask - Enhanced Q&A with RAG
{
    "question": "What happened to the Knights Radiant?",
    "use_rag": true,
    "retrieval_strategy": "hybrid",
    "context_length": 2000
}

# Response includes sources and retrieval info
{
    "answer": "Father Storm's response...",
    "sources_used": ["Knights of Wind and Truth", "Oathbringer"],
    "retrieval_info": {
        "chunks_retrieved": 5,
        "avg_similarity": 0.85,
        "strategy": "hybrid"
    }
}
```

## 📊 API Endpoints

### Core Endpoints

- `POST /ask` - Enhanced Q&A with RAG support
- `POST /add_document` - Add new books to the system
- `GET /search` - Direct document search (testing)
- `GET /health` - System health check
- `GET /stats` - RAG system statistics

### Example Usage

```bash
# Ask a question with RAG
curl -X POST "http://localhost:8000/ask" \
  -H "Content-Type: application/json" \
  -d '{"question": "What are the Windrunner ideals?"}'

# Add a new document
curl -X POST "http://localhost:8000/add_document" \
  -H "Content-Type: application/json" \
  -d '{
    "file_path": "/path/to/book.txt",
    "book_title": "Warbreaker",
    "book_number": 1
  }'

# Search documents directly
curl "http://localhost:8000/search?query=spren&k=3&strategy=semantic"
```

## ⚙️ Configuration Options

### Vector Store Selection

Choose based on your needs:

```python
# Development - Fast setup, no persistence
vector_store = InMemoryVectorStore()

# Production - Fast search, large datasets
vector_store = FAISSVectorStore(dimension=1536)

# Advanced - Metadata filtering, SQL queries
vector_store = SQLiteVectorStore("database.db")
```

### Retrieval Parameters

Fine-tune retrieval behavior:

```python
service = RetrievalService(
    vector_store=store,
    top_k=5  # Number of chunks to retrieve
)

# Two-stage retrieval
result = service.retrieve_with_reranking(
    query="your question",
    initial_k=20,  # Initial broad search
    final_k=5      # Final refined results
)
```

### Context Management

Control how context is prepared:

```python
context = service.create_context(
    chunks=retrieved_chunks,
    max_length=2000  # Maximum context length
)
```

## 🔍 Query Expansion

Built-in Stormlight Archive terminology expansion:

```python
query = "What are spren?"
# Automatically expands to include: spren, spirit, essence, bond

query = "Tell me about Kaladin"
# Expands to: kaladin, stormblessed, windrunner
```

## 📈 Performance Considerations

### Vector Store Performance

| Store Type | Setup Time | Query Speed | Memory Usage | Persistence |
|------------|------------|-------------|--------------|-------------|
| InMemory   | Fast       | Good        | High         | Manual      |
| FAISS      | Medium     | Excellent   | Medium       | Automatic   |
| SQLite     | Medium     | Good        | Low          | Automatic   |

### Optimization Tips

1. **Chunk Size**: 1000 tokens with 200 overlap works well for narrative text
2. **Embeddings**: Use OpenAI's `text-embedding-ada-002` for best results
3. **Retrieval**: Start with k=5, increase for complex queries
4. **Context**: Limit to 2000 characters to stay within token limits

## 🛠️ Customization

### Adding New Books

```python
# Process any Brandon Sanderson book
processor = BookProcessor()
chunks = processor.process_document(
    content=book_text,
    metadata={
        "source": "Warbreaker",
        "series": "Standalone",
        "author": "Brandon Sanderson",
        "universe": "Cosmere"
    }
)
```

### Custom Query Processing

```python
class CustomQueryProcessor(QueryProcessor):
    def expand_query(self, query: str) -> List[str]:
        # Add your own terminology expansions
        return expanded_terms
```

### Custom Reranking

```python
def custom_rerank_chunks(self, query: str, results: List[tuple]) -> List[tuple]:
    # Implement your own scoring logic
    return reranked_results
```

## 📝 Example Queries

The system handles various types of questions:

### Character Information
- "Who is Kaladin Stormblessed?"
- "What happened to Dalinar in Oathbringer?"
- "Tell me about Shallan's multiple personalities"

### World Building
- "What are the Knights Radiant orders?"
- "How does surgebinding work?"
- "What is the Cognitive Realm?"

### Plot Events
- "What happened during the Battle of Thaylen Field?"
- "Why were the Radiants imprisoned?"
- "What is the Everstorm?"

### Magic System
- "How do spren bonds work?"
- "What are the Windrunner ideals?"
- "What is a Shardblade?"

## 🔧 Troubleshooting

### Common Issues

1. **No embeddings created**: Check OpenAI API key
2. **Slow retrieval**: Consider using FAISS for large datasets
3. **Poor results**: Increase chunk overlap or adjust query expansion
4. **Memory issues**: Use SQLite store for large document collections

### Debug Mode

Enable detailed logging:

```python
import logging
logging.basicConfig(level=logging.DEBUG)

# This will show retrieval details and similarity scores
```

## 🤝 Contributing

To extend the system:

1. **Add new vector stores**: Implement the `VectorStore` interface
2. **Improve retrieval**: Enhance the reranking algorithms
3. **Expand query processing**: Add more terminology mappings
4. **Add new endpoints**: Extend the FastAPI application

## 📚 Further Reading

- [OpenAI Embeddings Guide](https://platform.openai.com/docs/guides/embeddings)
- [FAISS Documentation](https://faiss.ai/)
- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [RAG Architecture Patterns](https://docs.llamaindex.ai/en/stable/getting_started/concepts.html)

## ⚡ What's Next?

This RAG implementation provides a solid foundation. To integrate it into your existing `app.py`, you would:

1. **Replace the simple prompt** with the RAG-enhanced system
2. **Add document processing** to populate your vector store
3. **Choose appropriate vector store** based on your scale
4. **Fine-tune retrieval parameters** for your use case

The modular design makes it easy to adopt incrementally - you can start with the in-memory store and upgrade to FAISS as your dataset grows! 