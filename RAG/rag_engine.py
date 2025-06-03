from sentence_transformers import SentenceTransformer
import faiss
import numpy as np
from typing import List, Dict
import json
import os

class RAGEngine:
    def __init__(self, model_name: str = "all-MiniLM-L6-v2"):
        self.model = SentenceTransformer(model_name)
        self.index = None
        self.documents = []
        
    def add_documents(self, documents: List[str]):
        """Add documents to the RAG system"""
        self.documents.extend(documents)
        embeddings = self.model.encode(documents)
        
        # Initialize FAISS index if not exists
        if self.index is None:
            self.index = faiss.IndexFlatL2(embeddings.shape[1])
        
        # Add embeddings to index
        self.index.add(np.array(embeddings).astype('float32'))
    
    def search(self, query: str, k: int = 3) -> List[str]:
        """Search for relevant documents"""
        if not self.index:
            return []
            
        # Encode query
        query_embedding = self.model.encode([query])
        
        # Search in FAISS
        distances, indices = self.index.search(
            np.array(query_embedding).astype('float32'), k
        )
        
        # Return relevant documents
        return [self.documents[i] for i in indices[0]]
    
    def save(self, path: str):
        """Save the RAG system to disk"""
        if not os.path.exists(path):
            os.makedirs(path)
            
        # Save documents
        with open(os.path.join(path, "documents.json"), "w") as f:
            json.dump(self.documents, f)
            
        # Save FAISS index
        faiss.write_index(self.index, os.path.join(path, "index.faiss"))
    
    def load(self, path: str):
        """Load the RAG system from disk"""
        # Load documents
        with open(os.path.join(path, "documents.json"), "r") as f:
            self.documents = json.load(f)
            
        # Load FAISS index
        self.index = faiss.read_index(os.path.join(path, "index.faiss")) 