import numpy as np
import json
import pickle
from typing import List, Dict, Any, Optional, Tuple
from abc import ABC, abstractmethod
from rag_document_processor import DocumentChunk
import sqlite3
import faiss

class VectorStore(ABC):
    """Abstract base class for vector stores."""
    
    @abstractmethod
    def add_documents(self, chunks: List[DocumentChunk]) -> None:
        """Add document chunks to the vector store."""
        pass
    
    @abstractmethod
    def search(self, query_embedding: List[float], k: int = 5) -> List[Tuple[DocumentChunk, float]]:
        """Search for similar documents."""
        pass
    
    @abstractmethod
    def save(self, path: str) -> None:
        """Save the vector store to disk."""
        pass
    
    @abstractmethod
    def load(self, path: str) -> None:
        """Load the vector store from disk."""
        pass

class InMemoryVectorStore(VectorStore):
    """Simple in-memory vector store using cosine similarity."""
    
    def __init__(self):
        self.chunks: List[DocumentChunk] = []
        self.embeddings: np.ndarray = None
        
    def add_documents(self, chunks: List[DocumentChunk]) -> None:
        """Add document chunks to the vector store."""
        self.chunks.extend(chunks)
        
        # Convert embeddings to numpy array
        embeddings = [chunk.embedding for chunk in chunks]
        if embeddings:
            new_embeddings = np.array(embeddings)
            if self.embeddings is None:
                self.embeddings = new_embeddings
            else:
                self.embeddings = np.vstack([self.embeddings, new_embeddings])
    
    def cosine_similarity(self, a: np.ndarray, b: np.ndarray) -> float:
        """Calculate cosine similarity between two vectors."""
        return np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b))
    
    def search(self, query_embedding: List[float], k: int = 5) -> List[Tuple[DocumentChunk, float]]:
        """Search for similar documents using cosine similarity."""
        if self.embeddings is None or len(self.chunks) == 0:
            return []
        
        query_vector = np.array(query_embedding)
        similarities = []
        
        for i, embedding in enumerate(self.embeddings):
            similarity = self.cosine_similarity(query_vector, embedding)
            similarities.append((self.chunks[i], similarity))
        
        # Sort by similarity (descending) and return top k
        similarities.sort(key=lambda x: x[1], reverse=True)
        return similarities[:k]
    
    def save(self, path: str) -> None:
        """Save the vector store to disk."""
        data = {
            'chunks': self.chunks,
            'embeddings': self.embeddings.tolist() if self.embeddings is not None else None
        }
        with open(path, 'wb') as f:
            pickle.dump(data, f)
    
    def load(self, path: str) -> None:
        """Load the vector store from disk."""
        with open(path, 'rb') as f:
            data = pickle.load(f)
        
        self.chunks = data['chunks']
        self.embeddings = np.array(data['embeddings']) if data['embeddings'] else None

class FAISSVectorStore(VectorStore):
    """FAISS-based vector store for better performance with large datasets."""
    
    def __init__(self, dimension: int = 1536):
        self.dimension = dimension
        self.index = faiss.IndexFlatIP(dimension)  # Inner product index
        self.chunks: List[DocumentChunk] = []
        
    def add_documents(self, chunks: List[DocumentChunk]) -> None:
        """Add document chunks to the FAISS index."""
        if not chunks:
            return
            
        embeddings = np.array([chunk.embedding for chunk in chunks]).astype('float32')
        
        # Normalize vectors for cosine similarity using inner product
        faiss.normalize_L2(embeddings)
        
        self.index.add(embeddings)
        self.chunks.extend(chunks)
    
    def search(self, query_embedding: List[float], k: int = 5) -> List[Tuple[DocumentChunk, float]]:
        """Search using FAISS index."""
        if len(self.chunks) == 0:
            return []
        
        query_vector = np.array([query_embedding]).astype('float32')
        faiss.normalize_L2(query_vector)
        
        scores, indices = self.index.search(query_vector, min(k, len(self.chunks)))
        
        results = []
        for score, idx in zip(scores[0], indices[0]):
            if idx >= 0:  # Valid index
                results.append((self.chunks[idx], float(score)))
        
        return results
    
    def save(self, path: str) -> None:
        """Save FAISS index and chunks to disk."""
        faiss.write_index(self.index, f"{path}.faiss")
        
        with open(f"{path}.chunks", 'wb') as f:
            pickle.dump(self.chunks, f)
    
    def load(self, path: str) -> None:
        """Load FAISS index and chunks from disk."""
        self.index = faiss.read_index(f"{path}.faiss")
        
        with open(f"{path}.chunks", 'rb') as f:
            self.chunks = pickle.load(f)

class SQLiteVectorStore(VectorStore):
    """SQLite-based vector store with metadata filtering capabilities."""
    
    def __init__(self, db_path: str = "vectors.db"):
        self.db_path = db_path
        self.init_db()
        
    def init_db(self):
        """Initialize SQLite database."""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS document_chunks (
                id TEXT PRIMARY KEY,
                content TEXT NOT NULL,
                embedding BLOB NOT NULL,
                metadata TEXT NOT NULL
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def add_documents(self, chunks: List[DocumentChunk]) -> None:
        """Add document chunks to SQLite database."""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        for chunk in chunks:
            embedding_blob = pickle.dumps(chunk.embedding)
            metadata_json = json.dumps(chunk.metadata)
            
            cursor.execute('''
                INSERT OR REPLACE INTO document_chunks 
                (id, content, embedding, metadata) 
                VALUES (?, ?, ?, ?)
            ''', (chunk.id, chunk.content, embedding_blob, metadata_json))
        
        conn.commit()
        conn.close()
    
    def search(self, query_embedding: List[float], k: int = 5, 
               metadata_filter: Optional[Dict[str, Any]] = None) -> List[Tuple[DocumentChunk, float]]:
        """Search with optional metadata filtering."""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Build WHERE clause for metadata filtering
        where_clause = ""
        params = []
        
        if metadata_filter:
            conditions = []
            for key, value in metadata_filter.items():
                conditions.append(f"json_extract(metadata, '$.{key}') = ?")
                params.append(value)
            where_clause = "WHERE " + " AND ".join(conditions)
        
        cursor.execute(f"SELECT * FROM document_chunks {where_clause}", params)
        rows = cursor.fetchall()
        
        conn.close()
        
        if not rows:
            return []
        
        # Calculate similarities
        query_vector = np.array(query_embedding)
        similarities = []
        
        for row in rows:
            chunk_id, content, embedding_blob, metadata_json = row
            embedding = pickle.loads(embedding_blob)
            metadata = json.loads(metadata_json)
            
            chunk = DocumentChunk(
                id=chunk_id,
                content=content,
                metadata=metadata,
                embedding=embedding
            )
            
            similarity = np.dot(query_vector, embedding) / (
                np.linalg.norm(query_vector) * np.linalg.norm(embedding)
            )
            similarities.append((chunk, similarity))
        
        # Sort by similarity and return top k
        similarities.sort(key=lambda x: x[1], reverse=True)
        return similarities[:k]
    
    def save(self, path: str) -> None:
        """SQLite data is already persisted."""
        pass
    
    def load(self, path: str) -> None:
        """SQLite data is loaded on init."""
        pass 