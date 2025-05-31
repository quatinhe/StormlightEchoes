using UnityEngine;

public class enemyScript : MonoBehaviour
{

    public GameObject plataforma;
    public Vector2 nodoEnemigo;
    private creadorDeNodosScript creadorDeNodos;
    private int idEnemigo;
    private bool sePuedeActualizar;
    private Rigidbody2D rigidbody;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        sePuedeActualizar = true;
        creadorDeNodos = FindAnyObjectByType<creadorDeNodosScript>();
        string nombre = gameObject.name; // "NodoX"
        if (nombre.StartsWith("enemy"))
            int.TryParse(nombre.Substring(5), out idEnemigo);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("plataform") && rigidbody.linearVelocityY < 1)
        {
            plataforma = collision.gameObject;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                nodoEnemigo = contact.point;
                nodoEnemigo = new Vector2(rigidbody.position.x, nodoEnemigo.y);
                creadorDeNodos.actualizarPuntoContactoEnemigo(idEnemigo,nodoEnemigo, plataforma);
                creadorDeNodos.setProximoNodoEnemyPiso(idEnemigo, true);// aca puede haber un error, capaz hay que esperar un poco a que salte
                return;
            }
        }
    }

}
