import openai
from typing import List, Dict, Any, Optional
from dataclasses import dataclass
from rag_document_processor import DocumentProcessor, DocumentChunk
from rag_vector_store import VectorStore, InMemoryVectorStore

@dataclass
class RetrievalResult:
    query: str
    retrieved_chunks: List[DocumentChunk]
    similarities: List[float]
    context: str

class QueryProcessor:
    """Handles query preprocessing and expansion."""
    
    def __init__(self):
        self.processor = DocumentProcessor()
    
    def preprocess_query(self, query: str) -> str:
        """Clean and preprocess the query."""
        return query.strip().lower()
    
    def expand_query(self, query: str) -> List[str]:
        """Expand query with synonyms and related terms for Stormlight Archive."""
        expansions = {
            "spren": ["spren", "spirit", "essence", "bond"],
            "surgebinding": ["surgebinding", "surge", "radiant powers", "magic"],
            "knights radiant": ["knights radiant", "radiants", "orders", "surgebinders"],
            "highstorm": ["highstorm", "storm", "stormwall", "everstorm"],
            "shardbearer": ["shardbearer", "shardblade", "shardplate", "shard"],
            "dalinar": ["dalinar", "blackthorn", "kholin"],
            "kaladin": ["kaladin", "stormblessed", "windrunner"],
            "shallan": ["shallan", "veil", "radiant", "lightweaver"],
            "adolin": ["adolin", "kholin", "duelist"],
            "navani": ["navani", "kholin", "artifabrian"],
            "roshar": ["roshar", "world", "planet", "continent"],
            "vorin": ["vorin", "vorinism", "religion", "almighty"],
        }
        
        query_lower = query.lower()
        expanded_terms = [query]
        
        for term, synonyms in expansions.items():
            if term in query_lower:
                expanded_terms.extend([syn for syn in synonyms if syn not in expanded_terms])
        
        return expanded_terms

class RetrievalService:
    """Main service for document retrieval and context preparation."""
    
    def __init__(self, vector_store: VectorStore = None, top_k: int = 5):
        self.vector_store = vector_store or InMemoryVectorStore()
        self.processor = DocumentProcessor()
        self.query_processor = QueryProcessor()
        self.top_k = top_k
    
    def create_query_embedding(self, query: str) -> List[float]:
        """Create embedding for the query."""
        embeddings = self.processor.create_embeddings([query])
        return embeddings[0]
    
    def retrieve_relevant_chunks(self, query: str, 
                                metadata_filter: Optional[Dict[str, Any]] = None) -> RetrievalResult:
        """Retrieve relevant document chunks for a query."""
        
        # Preprocess query
        processed_query = self.query_processor.preprocess_query(query)
        
        # Create query embedding
        query_embedding = self.create_query_embedding(processed_query)
        
        # Search vector store
        if hasattr(self.vector_store, 'search') and 'metadata_filter' in self.vector_store.search.__code__.co_varnames:
            # SQLite vector store supports metadata filtering
            results = self.vector_store.search(
                query_embedding, 
                k=self.top_k,
                metadata_filter=metadata_filter
            )
        else:
            # Other vector stores
            results = self.vector_store.search(query_embedding, k=self.top_k)
        
        chunks = [chunk for chunk, _ in results]
        similarities = [similarity for _, similarity in results]
        
        # Create context from retrieved chunks
        context = self.create_context(chunks)
        
        return RetrievalResult(
            query=query,
            retrieved_chunks=chunks,
            similarities=similarities,
            context=context
        )
    
    def create_context(self, chunks: List[DocumentChunk], max_length: int = 2000) -> str:
        """Create context string from retrieved chunks."""
        if not chunks:
            return ""
        
        context_parts = []
        current_length = 0
        
        for chunk in chunks:
            # Add source information
            source_info = f"[{chunk.metadata.get('source', 'Unknown')}]"
            chunk_text = f"{source_info}\n{chunk.content}\n"
            
            if current_length + len(chunk_text) > max_length and context_parts:
                break
            
            context_parts.append(chunk_text)
            current_length += len(chunk_text)
        
        return "\n---\n".join(context_parts)
    
    def retrieve_with_reranking(self, query: str, 
                               initial_k: int = 20,
                               final_k: int = 5) -> RetrievalResult:
        """Retrieve documents with two-stage retrieval and reranking."""
        
        # First stage: broad retrieval
        query_embedding = self.create_query_embedding(query)
        initial_results = self.vector_store.search(query_embedding, k=initial_k)
        
        if not initial_results:
            return RetrievalResult(
                query=query,
                retrieved_chunks=[],
                similarities=[],
                context=""
            )
        
        # Second stage: rerank using a more sophisticated method
        reranked_results = self.rerank_chunks(query, initial_results)
        
        # Take top final_k results
        final_results = reranked_results[:final_k]
        
        chunks = [chunk for chunk, _ in final_results]
        similarities = [similarity for _, similarity in final_results]
        context = self.create_context(chunks)
        
        return RetrievalResult(
            query=query,
            retrieved_chunks=chunks,
            similarities=similarities,
            context=context
        )
    
    def rerank_chunks(self, query: str, initial_results: List[tuple]) -> List[tuple]:
        """Rerank chunks based on multiple factors."""
        reranked = []
        
        for chunk, similarity in initial_results:
            # Calculate additional scoring factors
            content_score = self.calculate_content_relevance(query, chunk.content)
            metadata_score = self.calculate_metadata_relevance(query, chunk.metadata)
            
            # Combine scores (you can adjust weights)
            combined_score = (
                0.6 * similarity +  # Vector similarity
                0.3 * content_score +  # Content relevance
                0.1 * metadata_score   # Metadata relevance
            )
            
            reranked.append((chunk, combined_score))
        
        # Sort by combined score
        reranked.sort(key=lambda x: x[1], reverse=True)
        return reranked
    
    def calculate_content_relevance(self, query: str, content: str) -> float:
        """Calculate content relevance using keyword matching."""
        query_words = set(query.lower().split())
        content_words = set(content.lower().split())
        
        if not query_words:
            return 0.0
        
        # Simple overlap score
        overlap = len(query_words.intersection(content_words))
        return overlap / len(query_words)
    
    def calculate_metadata_relevance(self, query: str, metadata: Dict[str, Any]) -> float:
        """Calculate metadata relevance score."""
        score = 0.0
        
        # Boost certain sources
        source = metadata.get('source', '').lower()
        series = metadata.get('series', '').lower()
        
        if 'stormlight' in query.lower():
            if 'stormlight' in series:
                score += 0.5
        
        # Boost recent books for timeline questions
        if any(word in query.lower() for word in ['recent', 'latest', 'now', 'current']):
            book_number = metadata.get('book_number', 0)
            if book_number >= 3:  # Rhythm of War and beyond
                score += 0.3
        
        return score

class HybridRetrieval:
    """Combines multiple retrieval strategies."""
    
    def __init__(self, vector_store: VectorStore):
        self.retrieval_service = RetrievalService(vector_store)
        self.query_processor = QueryProcessor()
    
    def retrieve(self, query: str, strategy: str = "semantic") -> RetrievalResult:
        """Retrieve using specified strategy."""
        
        if strategy == "semantic":
            return self.retrieval_service.retrieve_relevant_chunks(query)
        
        elif strategy == "hybrid":
            return self.retrieval_service.retrieve_with_reranking(query)
        
        elif strategy == "keyword":
            return self.keyword_retrieval(query)
        
        else:
            raise ValueError(f"Unknown retrieval strategy: {strategy}")
    
    def keyword_retrieval(self, query: str) -> RetrievalResult:
        """Keyword-based retrieval as fallback."""
        # This would implement BM25 or similar keyword matching
        # For now, we'll use semantic retrieval with query expansion
        expanded_queries = self.query_processor.expand_query(query)
        
        all_results = []
        for expanded_query in expanded_queries:
            result = self.retrieval_service.retrieve_relevant_chunks(expanded_query)
            all_results.extend(zip(result.retrieved_chunks, result.similarities))
        
        # Remove duplicates and sort
        unique_results = {}
        for chunk, similarity in all_results:
            if chunk.id not in unique_results or unique_results[chunk.id][1] < similarity:
                unique_results[chunk.id] = (chunk, similarity)
        
        sorted_results = sorted(unique_results.values(), key=lambda x: x[1], reverse=True)
        top_results = sorted_results[:self.retrieval_service.top_k]
        
        chunks = [chunk for chunk, _ in top_results]
        similarities = [similarity for _, similarity in top_results]
        context = self.retrieval_service.create_context(chunks)
        
        return RetrievalResult(
            query=query,
            retrieved_chunks=chunks,
            similarities=similarities,
            context=context
        ) 