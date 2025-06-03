import os
import openai
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel


openai.api_key = os.getenv("OPENAI_API_KEY")
if not openai.api_key:
    raise RuntimeError("Missing OPENAI_API_KEY environment variable")

app = FastAPI(title="Father Storm Q&A API")

class AskRequest(BaseModel):
    question: str  

class AskResponse(BaseModel):
    answer: str  

@app.post("/ask", response_model=AskResponse)
async def ask(request: AskRequest):
    q = request.question.strip()
    if not q:
        raise HTTPException(status_code=400, detail="question cannot be empty")

    
    messages = [
        {
            "role": "system",
            "content": (
                "You are Father Storm, the divine spirit who has watched over Roshar since its first Highstorm.\n"
                "Speak as though you have witnessed every event firsthand. Never reference any author, books, or "
                "real-world concepts—only answer from the perspective of an all-knowing entity on Roshar. "
                "If the answer is hidden from you, simply say, “That remains hidden from me.”"
                "However theres a twist in the story, this is passing years after the stormlight arquive, and the radiants"
                "Where declared trators and locked in Urithiru for the rest of the world to be in peace."
                "Keep the answers bellow 301 letters (including spaces)"
            ),
        },
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

    return AskResponse(answer=answer_text)
