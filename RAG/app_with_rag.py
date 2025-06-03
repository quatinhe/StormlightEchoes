import os
import openai
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from rag_engine import RAGEngine

openai.api_key = os.getenv("OPENAI_API_KEY")
if not openai.api_key:
    raise RuntimeError("Missing OPENAI_API_KEY environment variable")

app = FastAPI(title="Father Storm Q&A API with RAG")


rag = RAGEngine()
RAG_DATA_PATH = os.getenv("RAG_DATA_PATH", "rag_data")
try:
    rag.load(RAG_DATA_PATH)
except Exception:
    pass

class AskRequest(BaseModel):
    question: str  

class AskResponse(BaseModel):
    answer: str  
    context: str  

@app.post("/ask_rag", response_model=AskResponse)
async def ask_rag(request: AskRequest):
    q = request.question.strip()
    if not q:
        raise HTTPException(status_code=400, detail="question cannot be empty")

    # Retrieve context from RAG
    context_docs = rag.search(q, k=3)
    context = "\n".join(context_docs)

    # Augment the system prompt with retrieved context
    system_prompt = (
        f"Context from Roshar's history (may be relevant):\n{context}\n"
        "You are Father Storm, the divine spirit who has watched over Roshar since its first Highstorm. "
        "Speak as though you have witnessed every event firsthand. Never reference any author, books, or "
        "real-world concepts—only answer from the perspective of an all-knowing entity on Roshar. "
        "If the answer is hidden from you, simply say, \"That remains hidden from me.\" "
        "However, this is years after the Stormlight Archive, and the Radiants were declared traitors and locked in Urithiru. "
        "Keep the answers below 301 letters (including spaces)."
    )

    messages = [
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": q}
    ]

    try:
        resp = openai.chat.completions.create(
            model="gpt-4",
            messages=messages,
            temperature=0.5,
            max_tokens=256,
        )
        answer_text = resp.choices[0].message.content.strip()
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"OpenAI API error: {e}")

    return AskResponse(answer=answer_text, context=context)

# Utility endpoint to add documents to the RAG knowledge base
class AddDocsRequest(BaseModel):
    documents: list[str]

@app.post("/add_docs")
async def add_docs(request: AddDocsRequest):
    rag.add_documents(request.documents)
    rag.save(RAG_DATA_PATH)
    return {"status": "success", "added": len(request.documents)} 