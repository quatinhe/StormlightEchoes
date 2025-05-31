using UnityEngine;

public class playerScript : MonoBehaviour
{
    public GameObject plataforma;
    public Vector2 nodoPlayer;
    private creadorDeNodosScript creadorDeNodos;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    // Update is called once per frame
    void Update()
    {
        creadorDeNodos = FindAnyObjectByType<creadorDeNodosScript>();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("plataform"))
        {
            plataforma = collision.gameObject;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector2 posicion = new Vector2(rb.position.x, contact.point.y);
                creadorDeNodos.setPuntoContactoPlayer(posicion, plataforma);
                return;
            }
        }
    }
}
