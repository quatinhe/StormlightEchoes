#!/usr/bin/env python3
"""
Setup script for initializing the RAG system with Stormlight Archive content.
This script shows how to process books and populate the vector store.
"""

import os
import sys
from pathlib import Path
from rag_document_processor import BookProcessor, DocumentProcessor
from rag_vector_store import InMemoryVectorStore, FAISSVectorStore, SQLiteVectorStore

def setup_sample_content():
    """Create sample Stormlight Archive content for testing."""
    
    sample_content = {
        "The Way of Kings": """
        Kaladin stood at the edge of the precipice, looking down at the chasmfiends below. 
        The creatures moved like ancient behemoths through the chasms of the Shattered Plains.
        He gripped his spear tighter, feeling the weight of responsibility on his shoulders.
        
        "Bridge Four!" he called out to his men. They looked up at him with hope in their eyes,
        something that had been absent for so long. These bridgemen had been treated as
        expendable, but Kaladin saw them as brothers.
        
        The Windrunner powers stirred within him as he prepared for the assault.
        Syl danced around his head, the honorspren finally able to manifest more fully
        due to their strengthening bond. The Words had been spoken, the oath sworn.
        
        Dalinar Kholin watched from the command tent as his nephew Adolin prepared
        for another duel in the arena. The young man's skill with the Shardblade
        was impressive, but Dalinar worried about the political implications.
        
        Meanwhile, in Kharbranth, Shallan Davar studied the strange spren that
        seemed drawn to her sketches. Pattern hummed softly as she worked,
        though she didn't yet understand what the creature truly was.
        """,
        
        "Words of Radiance": """
        The Everstorm approached from the wrong direction, bringing with it 
        red lightning and destruction. Kaladin soared through the air on
        winds made manifest, his Windrunner abilities now fully awakened.
        
        Shallan had learned to embrace all aspects of herself - not just
        the scholar, but Veil the thief and Radiant the warrior. Her 
        Lightweaver powers allowed her to become anyone, but finding
        her true self remained the greatest challenge.
        
        Dalinar united the highprinces against a common threat, but the
        visions from the Almighty continued to plague him. Honor was dead,
        and something else stirred in the spiritual realm beyond.
        
        The Knights Radiant were returning, but so were their ancient enemies.
        The Parshendi had transformed into something new and terrible,
        empowered by the false gods they called the Fused.
        """,
        
        "Oathbringer": """
        Dalinar stood before the Thrill incarnate, facing Nergaoul itself.
        The Blackthorn's past came back to haunt him as he remembered
        the atrocities he had committed in his younger days.
        
        "I am Unity," he declared, binding together the realms through
        his Bondsmith powers. The revelation that he was connected to
        the Stormfather changed everything about how he understood his role.
        
        Kaladin struggled with his oath progression, unable to speak the
        Fourth Ideal due to his inability to forgive those who had hurt
        people he cared about. The darkness within him warred with
        his desire to protect.
        
        Shallan ventured into Shadesmar, the Cognitive Realm, discovering
        the true nature of spren and their connection to the Physical Realm.
        The secrets of the ancient Radiants slowly unraveled.
        
        Szeth-son-son-Vallano, truthless of Shinovar, wielded Nightblood
        and grappled with the truth about his past and the lies that
        had defined his existence.
        """,
        
        "Rhythm of War": """
        The tower of Urithiru had fallen to the enemy, its defenses compromised
        by the Lady of Wishes herself. Raboniel, the ancient Fused researcher,
        conducted her experiments with a cold scientific precision.
        
        Navani worked alongside her captor, advancing the understanding of
        fabrial technology while searching for a way to resist the occupation.
        The discoveries they made together would change the nature of magic itself.
        
        Kaladin descended into the depths of his own mind, confronting
        the depression that had plagued him throughout his life. In the
        darkness of the tower's basement levels, he found both despair and hope.
        
        Venli learned to navigate her new existence as a Radiant, bonded
        to Timbre the Reacher spren. Her journey from villain to hero
        was fraught with guilt over her role in bringing about the Everstorm.
        
        The war between humans and singers reached new heights of destruction,
        while ancient secrets about the true nature of Roshar were revealed.
        """,
        
        "Knights of Wind and Truth": """
        Years have passed since the end of the contest of champions.
        The decision was made by the coalition of monarchs: the Knights Radiant
        had grown too powerful, too dangerous for the common people.
        
        The tower of Urithiru, once a beacon of hope, now serves as a prison
        for those who once wielded Shardblades and commanded the storms.
        The spren bonds have been severed through terrible means, leaving
        the former Radiants powerless and isolated.
        
        Kaladin stares out through the crystal windows of his cell, watching
        the storms pass by without feeling their call. The Windrunner who
        once soared through the skies now walks on feet of clay.
        
        Shallan's multiple personalities have merged into one, but the process
        has left her diminished. Her illusions are gone, leaving only
        memories of what she once could create.
        
        Dalinar's connection to the Stormfather was cut violently, leaving
        him a broken man who speaks to empty air, hoping for a response
        that will never come. The Bondsmith who once united realms now
        cannot even unite his own fractured mind.
        
        In this new age of peace, the common people prosper while their
        former saviors waste away in their gilded cage. The very power
        that once protected Roshar is now seen as its greatest threat.
        """
    }
    
    # Create sample files
    sample_dir = Path("sample_books")
    sample_dir.mkdir(exist_ok=True)
    
    for i, (title, content) in enumerate(sample_content.items(), 1):
        file_path = sample_dir / f"{title.replace(' ', '_').lower()}.txt"
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Created sample file: {file_path}")
    
    return sample_dir

def initialize_vector_store(store_type: str = "memory"):
    """Initialize the chosen vector store type."""
    
    if store_type == "memory":
        return InMemoryVectorStore()
    elif store_type == "faiss":
        return FAISSVectorStore()
    elif store_type == "sqlite":
        return SQLiteVectorStore("stormlight_archive.db")
    else:
        raise ValueError(f"Unknown vector store type: {store_type}")

def process_documents(sample_dir: Path, vector_store):
    """Process all sample documents and add them to the vector store."""
    
    processor = BookProcessor()
    
    book_info = [
        ("the_way_of_kings.txt", "The Way of Kings", 1),
        ("words_of_radiance.txt", "Words of Radiance", 2),
        ("oathbringer.txt", "Oathbringer", 3),
        ("rhythm_of_war.txt", "Rhythm of War", 4),
        ("knights_of_wind_and_truth.txt", "Knights of Wind and Truth", 5)
    ]
    
    total_chunks = 0
    
    for filename, title, book_number in book_info:
        file_path = sample_dir / filename
        
        if file_path.exists():
            print(f"Processing {title}...")
            
            try:
                chunks = processor.process_stormlight_archive(
                    str(file_path), title, book_number
                )
                
                vector_store.add_documents(chunks)
                total_chunks += len(chunks)
                
                print(f"  Added {len(chunks)} chunks from {title}")
                
            except Exception as e:
                print(f"  Error processing {title}: {e}")
        else:
            print(f"  File not found: {file_path}")
    
    print(f"\nTotal chunks processed: {total_chunks}")
    return total_chunks

def test_retrieval(vector_store):
    """Test the retrieval system with sample queries."""
    
    from rag_retrieval_service import RetrievalService
    
    retrieval_service = RetrievalService(vector_store)
    
    test_queries = [
        "Who is Kaladin?",
        "What are the Knights Radiant?",
        "What happened to the Radiants?",
        "Tell me about Windrunners",
        "What is Urithiru?",
        "What is surgebinding?"
    ]
    
    print("\n" + "="*50)
    print("TESTING RETRIEVAL SYSTEM")
    print("="*50)
    
    for query in test_queries:
        print(f"\nQuery: {query}")
        print("-" * 30)
        
        try:
            result = retrieval_service.retrieve_relevant_chunks(query)
            
            if result.retrieved_chunks:
                print(f"Found {len(result.retrieved_chunks)} relevant chunks:")
                for i, (chunk, similarity) in enumerate(zip(result.retrieved_chunks, result.similarities)):
                    print(f"  {i+1}. [{chunk.metadata.get('source', 'Unknown')}] "
                          f"(similarity: {similarity:.3f})")
                    print(f"     {chunk.content[:100]}...")
            else:
                print("No relevant chunks found.")
                
        except Exception as e:
            print(f"Error during retrieval: {e}")

def save_vector_store(vector_store, store_type: str):
    """Save the populated vector store to disk."""
    
    save_path = f"stormlight_archive_{store_type}"
    
    try:
        vector_store.save(save_path)
        print(f"\nVector store saved to: {save_path}")
    except Exception as e:
        print(f"Error saving vector store: {e}")

def main():
    """Main setup function."""
    
    print("Stormlight Archive RAG System Setup")
    print("="*40)
    
    # Choose vector store type
    store_type = input("Choose vector store type (memory/faiss/sqlite) [memory]: ").strip().lower() or "memory"
    
    try:
        # Step 1: Create sample content
        print("\n1. Creating sample content...")
        sample_dir = setup_sample_content()
        
        # Step 2: Initialize vector store
        print(f"\n2. Initializing {store_type} vector store...")
        vector_store = initialize_vector_store(store_type)
        
        # Step 3: Process documents
        print("\n3. Processing documents...")
        total_chunks = process_documents(sample_dir, vector_store)
        
        if total_chunks > 0:
            # Step 4: Test retrieval
            print("\n4. Testing retrieval system...")
            test_retrieval(vector_store)
            
            # Step 5: Save vector store
            print("\n5. Saving vector store...")
            save_vector_store(vector_store, store_type)
            
            print("\n" + "="*50)
            print("SETUP COMPLETE!")
            print("="*50)
            print(f"✓ Created {total_chunks} document chunks")
            print(f"✓ Initialized {store_type} vector store")
            print("✓ Tested retrieval system")
            print("✓ Saved vector store to disk")
            print("\nYou can now run the RAG-enabled FastAPI app!")
            
        else:
            print("\nNo documents were processed successfully.")
            
    except Exception as e:
        print(f"\nSetup failed: {e}")
        return 1
    
    return 0

if __name__ == "__main__":
    sys.exit(main()) 