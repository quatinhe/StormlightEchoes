# Father Storm Q&A API with RAG

This project provides a FastAPI-based Q&A API for the world of Roshar, featuring a Retrieval-Augmented Generation (RAG) system to enhance answers with your own knowledge base. It uses OpenAI's GPT-4 for generation and FAISS + Sentence Transformers for retrieval.

## Features
- **/ask_rag**: Ask questions and get answers from "Father Storm" with context retrieved from your custom knowledge base.
- **/add_docs**: Add new documents to the RAG knowledge base.

## Setup

### 1. Install dependencies
```bash
pip install -r requirements.txt
```

### 2. Set your OpenAI API key
Set the `OPENAI_API_KEY` environment variable:
```bash
export OPENAI_API_KEY=sk-...   # On Linux/macOS
set OPENAI_API_KEY=sk-...      # On Windows
```

Optionally, set a custom path for RAG data:
```bash
export RAG_DATA_PATH=your_rag_data_folder
```

### 3. Run the API
```bash
uvicorn app_with_rag:app --reload
```

## Usage

### Add Documents to the Knowledge Base
Before asking questions, add documents to the RAG system:

**POST /add_docs**
```json
{
  "documents": [
    "The Everstorm first appeared in the east.",
    "Urithiru is the ancient city of the Knights Radiant.",
    "Stormlight is the essence of power in Roshar."
  ]
}
```

Example with `curl`:
```bash
curl -X POST "http://localhost:8000/add_docs" -H "Content-Type: application/json" -d '{"documents": ["The Everstorm first appeared in the east.", "Urithiru is the ancient city of the Knights Radiant.", "Stormlight is the essence of power in Roshar."]}'
```

### Ask a Question with RAG
**POST /ask_rag**
```json
{
  "question": "What is Urithiru?"
}
```

Example with `curl`:
```bash
curl -X POST "http://localhost:8000/ask_rag" -H "Content-Type: application/json" -d '{"question": "What is Urithiru?"}'
```

**Response:**
```json
{
  "answer": "Urithiru is the ancient city where the Radiants were confined, a place of power and secrets, now silent as storms rage outside.",
  "context": "Urithiru is the ancient city of the Knights Radiant."
}
```

## How it Works
1. **Document Ingestion**: Add lore, facts, or any text to the RAG system using `/add_docs`.
2. **Retrieval**: When you ask a question, the RAG system finds the most relevant documents using semantic search.
3. **Augmented Generation**: The retrieved context is included in the prompt sent to OpenAI's GPT-4, improving answer accuracy and grounding.

## File Overview
- `app_with_rag.py`: FastAPI app with RAG-powered Q&A endpoints.
- `rag_engine.py`: Simple RAG engine using Sentence Transformers and FAISS.
- `requirements.txt`: All dependencies.

## Notes
- The original `app.py` is untouched and uses only OpenAI for answers.
- The RAG system persists its data in the folder specified by `RAG_DATA_PATH` (default: `rag_data`).
- You can add more documents at any time; they will be saved and used for future questions.

## Troubleshooting
- If you get errors about missing FAISS or sentence-transformers, ensure you installed all requirements.
- If you change the knowledge base, always use `/add_docs` and the data will be saved automatically.

---
Enjoy your custom Roshar Q&A API with Retrieval-Augmented Generation! 