import os
import openai
from typing import List, Dict, Any
import tiktoken
from dataclasses import dataclass
import hashlib

@dataclass
class DocumentChunk:
    id: str
    content: str
    metadata: Dict[str, Any]
    embedding: List[float] = None

class DocumentProcessor:
    def __init__(self, model_name: str = "text-embedding-ada-002"):
        self.model_name = model_name
        self.encoding = tiktoken.get_encoding("cl100k_base")
        
    def chunk_text(self, text: str, chunk_size: int = 1000, overlap: int = 200) -> List[str]:
        """Split text into overlapping chunks based on token count."""
        tokens = self.encoding.encode(text)
        chunks = []
        
        start = 0
        while start < len(tokens):
            end = start + chunk_size
            chunk_tokens = tokens[start:end]
            chunk_text = self.encoding.decode(chunk_tokens)
            chunks.append(chunk_text)
            
            if end >= len(tokens):
                break
                
            start = end - overlap
            
        return chunks
    
    def create_embeddings(self, texts: List[str]) -> List[List[float]]:
        """Create embeddings for a list of texts."""
        try:
            response = openai.embeddings.create(
                model=self.model_name,
                input=texts
            )
            return [item.embedding for item in response.data]
        except Exception as e:
            raise Exception(f"Failed to create embeddings: {e}")
    
    def process_document(self, content: str, metadata: Dict[str, Any]) -> List[DocumentChunk]:
        """Process a document into chunks with embeddings."""
        chunks = self.chunk_text(content)
        embeddings = self.create_embeddings(chunks)
        
        document_chunks = []
        for i, (chunk, embedding) in enumerate(zip(chunks, embeddings)):
            chunk_id = hashlib.md5(f"{metadata.get('source', '')}-{i}-{chunk[:50]}".encode()).hexdigest()
            
            document_chunks.append(DocumentChunk(
                id=chunk_id,
                content=chunk,
                metadata={
                    **metadata,
                    "chunk_index": i,
                    "total_chunks": len(chunks)
                },
                embedding=embedding
            ))
        
        return document_chunks

class BookProcessor:
    def __init__(self):
        self.processor = DocumentProcessor()
        
    def process_stormlight_archive(self, book_path: str, book_title: str, book_number: int) -> List[DocumentChunk]:
        """Process a Stormlight Archive book."""
        with open(book_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        metadata = {
            "source": book_title,
            "series": "The Stormlight Archive",
            "book_number": book_number,
            "author": "Brandon Sanderson",
            "universe": "Cosmere"
        }
        
        return self.processor.process_document(content, metadata)
    
    def process_chapter(self, chapter_content: str, book_title: str, chapter_number: int) -> List[DocumentChunk]:
        """Process a single chapter."""
        metadata = {
            "source": f"{book_title} - Chapter {chapter_number}",
            "chapter": chapter_number,
            "book": book_title,
            "series": "The Stormlight Archive"
        }
        
        return self.processor.process_document(chapter_content, metadata) 