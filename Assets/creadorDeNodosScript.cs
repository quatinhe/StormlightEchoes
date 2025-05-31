using System.Collections.Generic;
using UnityEngine;
using System.Linq;//para ordenar las plataformas por la posicion  y

using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Xml.Schema;
using System.Collections;

public class creadorDeNodosScript : MonoBehaviour


{
    float tiempoTotal;
     
    Dictionary<int, List<CuatroEnteros>> mapa = new Dictionary<int, List<CuatroEnteros>>();
    Dictionary<int, List<CuatroEnteros>> mapaCheckpoint = new Dictionary<int, List<CuatroEnteros>>();


    public float maxVelocityInAir;
    public float velocityJump;
    public float velocityInGround;

    private bool actualizarCaminos;
    private GameObject[] plataformsObjects;
    private GameObject[] enemys;
    private float margenDesdeElBorde = 0.1f;
    private int contadorNodos = 0; 
    private Dictionary<int, Vector2> posicionesNodos = new Dictionary<int, Vector2>();
    private Dictionary<int, bool> actualizarPiso = new Dictionary<int, bool>();
    private Dictionary<int, int> nodosEnemgioId = new Dictionary<int, int>();
    private Dictionary<int, (Vector2, GameObject)> puntoContactoEnemigo = new Dictionary<int, (Vector2, GameObject)>();
    private (Vector2, GameObject) puntoContactoPlayer;
    private Dictionary<int, List<int>> caminoEnemigos = new Dictionary<int, List<int>>();
    private Dictionary<int, (bool, int, List<int>)> proximoNodoEnemy = new Dictionary<int, (bool, int, List<int>)>();// el bool es para saber si esta en el piso o en el aire 
    private int nodosPlataformasFijos;
    private bool hayCaminos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        actualizarCaminos = false;
        hayCaminos = false;
        tiempoTotal = 0;
        plataformsObjects = GameObject.FindGameObjectsWithTag("plataform");
        enemys = GameObject.FindGameObjectsWithTag("enemy");
        int idEnemigo = -1;
        
        foreach (GameObject enemigo in enemys) {
            string nombre = enemigo.name; // "NodoX"
            if (nombre.StartsWith("enemy"))
                int.TryParse(nombre.Substring(5), out idEnemigo);
            else { Debug.Log("ERROR, no encuentra enemigo"); }
            actualizarPiso[idEnemigo] = true;
        }
        foreach (GameObject go in plataformsObjects.OrderBy(g => g.transform.position.y))
        {

            Debug.Log("empezando para plataforma");

            Collider2D col = go.GetComponent<Collider2D>();
            if (col == null)
            {
                System.Console.WriteLine("error");
            }
            ;


            Bounds b = col.bounds;

            // Posiciones para nodos: cerca de los bordes superior izquierdo y derecho
            Vector3 izquierda = new Vector3(b.min.x + margenDesdeElBorde, b.max.y, go.transform.position.z);
            Vector3 derecha = new Vector3(b.max.x - margenDesdeElBorde, b.max.y, go.transform.position.z);


            GameObject nodoIzquierda = CrearNodoHijo(go.transform, izquierda);
            AgregarElemento(contadorNodos - 1, new CuatroEnteros(contadorNodos, 0, 0, (b.max.x - b.min.x - 2 * margenDesdeElBorde) / velocityInGround));

            GameObject nodoDerecha = CrearNodoHijo(go.transform, derecha);
            AgregarElemento(contadorNodos - 1, new CuatroEnteros(contadorNodos - 2, 0, 1, (b.max.x - b.min.x - 2 * margenDesdeElBorde) / velocityInGround));
            CrearNodoEnPlataformaDebajo(nodoIzquierda, 1);
            CrearNodoEnPlataformaDebajo(nodoDerecha, 0);
        }
        nodosPlataformasFijos = contadorNodos;
        mapaCheckpoint = CopiarMapa(mapa);


    }




    // Update is called once per frame
    void Update()
    {
        tiempoTotal += Time.deltaTime;

        

        if (tiempoTotal >= 5 || actualizarCaminos)
        {

            nodosEnemgioId = new Dictionary<int, int>(); 
            caminoEnemigos = new Dictionary<int, List<int>>();
            actualizarCaminos = false;
            tiempoTotal = 0;
            int v = nodosPlataformasFijos;
            contadorNodos = nodosPlataformasFijos;
            GameObject nodo = GameObject.Find("Nodo" + v);
            while ( nodo != null)
            { 
                DestroyImmediate(nodo);
                v++;
                nodo = GameObject.Find("Nodo" + v);
            }
            
                int r = nodosPlataformasFijos;
            while (posicionesNodos.ContainsKey(r))
            {
                posicionesNodos.Remove(r);
                r++;
                
            }
            mapa = CopiarMapa(mapaCheckpoint);
            Debug.Log("piso contiene a 1 ? " + actualizarPiso.ContainsKey(1));
            Debug.Log("piso contiene a 1 ? " + puntoContactoEnemigo.ContainsKey(1));
            foreach (var par in puntoContactoEnemigo) {
                Debug.Log("Se hace el nodo enemigo");
                Vector2 punto = par.Value.Item1;
                GameObject platafo = par.Value.Item2;
                nodosEnemgioId[par.Key] = contadorNodos;
                GameObject nodoEnemgioNuevo = CrearNodoHijo(platafo.transform, punto);
                actualizarCaminosPlataforma(nodoEnemgioNuevo, platafo);
            }

            Debug.Log("Se hace el nodo player");
            Vector2 puntoPlayer = puntoContactoPlayer.Item1; 
            GameObject platafoPlayer = puntoContactoPlayer.Item2;
            GameObject nodoPlayerNuevo = CrearNodoHijo(platafoPlayer.transform, puntoPlayer);
            actualizarCaminosPlataforma(nodoPlayerNuevo, platafoPlayer);

            for (int i = 0; i < contadorNodos; i++) 
            {
                Debug.Log("nodo inicial " + i);
                GameObject nodoI = GameObject.Find("Nodo" + i);
                if (nodoI == null)
                {
                    Debug.Log("Errorrrr en nodo " + i );
                    continue;
                }
                for (int l = 0; l < contadorNodos; l++)
                {


                    if (l == i)
                    {
                        continue;
                    }
                    

                    GameObject nodoL = GameObject.Find("Nodo" + l); 
                    if (nodoL == null)
                    {
                        Debug.Log("Errorrrr en nodo " + i);
                        continue;
                    }
                    


                    // Verificar que no tengan el mismo padre (misma plataforma)
                    if (nodoI.transform.parent == nodoL.transform.parent) continue;
                    Debug.Log("nodo final " + l);

                    // esto solo para ver la linea
                    Vector2 posI = nodoI.transform.position;
                    Vector2 posL = nodoL.transform.position;
                    Debug.Log("chequeando si es posible entre " + i + l);
                    var resultado = EsSaltoPosibleYVelocidad(posI, posL);
                    if (resultado.posible)
                    {


                        // Guardar el salto con dirección (velX) si querés
                        AgregarElemento(i, new CuatroEnteros(l, 1, resultado.velX, resultado.tiempo));
                        Debug.Log("salto es posible ");
                    }
                    else { Debug.Log("salto no es posible "); }
                }
            }




            dibujarLineas();


            int idEnemigo = -1;
            int nodoFinal = contadorNodos - 1;
            foreach (GameObject enemigo in enemys)
            {
                string nombre = enemigo.name; // "NodoX"
                if (nombre.StartsWith("enemy"))
                    int.TryParse(nombre.Substring(5), out idEnemigo);

                int nodoInicial = nodosEnemgioId[idEnemigo];
                
                List<int> lista = AStar(nodoInicial, nodoFinal);
                caminoEnemigos[idEnemigo] = lista;

                Debug.Log("imprimo camino desde hasta"+ nodoInicial + nodoFinal);
                imprimirCamino(nodoInicial, nodoFinal);//borrar estoooo!!!
                if (lista.Count() == 0)
                {
                    proximoNodoEnemy[idEnemigo] = (proximoNodoEnemy[idEnemigo].Item1, -1, lista);
                }
                else 
                {
                    int primerElemento = lista[0]; // Obtener el primer elemento
                    lista.RemoveAt(0);
                    imprimirCamino(nodoInicial, nodoFinal);//borrar estoooo!!!
                    proximoNodoEnemy[idEnemigo] = (proximoNodoEnemy[idEnemigo].Item1, primerElemento, lista);
                    Debug.Log("enemigo y primero nodo" + idEnemigo + primerElemento);
                }
                
            }
            hayCaminos = true;

        }

        else
        {
            if (hayCaminos)
            {
                int idEnemigo = -1;
                foreach (GameObject enemigo in enemys)
                {
                    Rigidbody2D rb = enemigo.GetComponent<Rigidbody2D>();
                    string nombre = enemigo.name; // "NodoX"
                    if (nombre.StartsWith("enemy"))
                        int.TryParse(nombre.Substring(5), out idEnemigo);
                    if (proximoNodoEnemy[idEnemigo].Item1) {
                         if (puntoContactoEnemigo[idEnemigo].Item2 == puntoContactoPlayer.Item2)
                        {
                            if (puntoContactoPlayer.Item1.x > enemigo.transform.position.x)
                            {
                                Debug.Log("Misma platafo, derecha");
                                rb.linearVelocity = new Vector2(velocityInGround, 0);
                            }
                            else
                            {
                                Debug.Log("Misma platafo, izquieda");
                                rb.linearVelocity = new Vector2(-velocityInGround, 0);
                            }
                        }
                    }
                    if (proximoNodoEnemy[idEnemigo].Item2 == -1)
                    {
                        if (proximoNodoEnemy[idEnemigo].Item1)
                        {
                            rb.linearVelocity = Vector2.zero;
                        }
                    } 
                    else
                    {
                        float pos = enemigo.transform.position.x;
                        int proximoNodo = proximoNodoEnemy[idEnemigo].Item2;
                        float posicionNodo = posicionesNodos[proximoNodo].x;
                        float escalaEnemigo = enemigo.transform.localScale.x * 0.5f;//!!!!!CAMBIAR ESTOOOO, CUANDO SE HAGA QUE EL NODO SALGA JUSTO ABAJO DEL ENEMIGO
                        bool estanCerca = (posicionNodo < pos + escalaEnemigo && posicionNodo > pos - escalaEnemigo);
                        if (estanCerca && proximoNodoEnemy[idEnemigo].Item1)
                        {

                            List<int> camino = proximoNodoEnemy[idEnemigo].Item3;
                            Debug.Log("se imprime tamanio lista ");
                            Debug.Log(camino.Count());
                            if (camino.Count == 0)
                            {

                            }
                            else
                            {
                                int nodoAIr = camino[0];
                                camino.RemoveAt(0);
                                proximoNodoEnemy[idEnemigo] = (proximoNodoEnemy[idEnemigo].Item1, nodoAIr, camino);

                                enemyGo(enemigo, proximoNodo, nodoAIr);
                            }
                            //lo proximo a hacer es ver porque no hace bien los caminos en linea recta, y luego probarlos
                        }
                    }
                }
            }
        }
    }

    private void enemyGo(GameObject enemigo, int nodoInicial, int nodoFinal) {
        Rigidbody2D rb = enemigo.GetComponent<Rigidbody2D>();
        CuatroEnteros datosParaIr = obtenerCuatroEnteros(nodoInicial, nodoFinal);
        string nombre = enemigo.name; // "NodoX"
        int idEnemigo = -1;
        if (nombre.StartsWith("enemy"))
            int.TryParse(nombre.Substring(5), out idEnemigo);
        if (datosParaIr.b == 0) {
            Debug.Log("b == 0 ,  va para " + datosParaIr.c);
            if (datosParaIr.c == 0)
            {

                rb.linearVelocity = new Vector2(velocityInGround, 0);
            }
            else {

                rb.linearVelocity = new Vector2(-velocityInGround, 0);

            }
        }
        if (datosParaIr.b == 1)
        {
            Debug.Log("b == 1 ,  velocidad: "+ datosParaIr.c);
            Debug.Log("b == 1 ,  velocity jump: " + velocityJump);
            rb.linearVelocity = new Vector2(datosParaIr.c, velocityJump);
            actualizarPiso[idEnemigo] = false;
            proximoNodoEnemy[idEnemigo] = (false, proximoNodoEnemy[idEnemigo].Item2, proximoNodoEnemy[idEnemigo].Item3);
            StartCoroutine(esperarYAsignarPisoTrue(idEnemigo, datosParaIr.d));
        }
        if (datosParaIr.b == 2)
        {
            Debug.Log("b == 2 ,  velocidad: " + datosParaIr.c);
            
            StartCoroutine(rutinaDeCaida( idEnemigo, enemigo, datosParaIr.c, datosParaIr.d));
            

        }
    }

    IEnumerator esperarYAsignarPisoTrue(int idEnemigo, float tiempoCaida)
    {
        yield return new WaitForSeconds(tiempoCaida - 0.05f);
        actualizarPiso[idEnemigo] = true; 
    }

        IEnumerator rutinaDeCaida(int idEnemigo, GameObject enemigo, float velocidad, float tiempoCaida)
    {
        Rigidbody2D rb = enemigo.GetComponent<Rigidbody2D>();
        actualizarPiso[idEnemigo] = false;
        proximoNodoEnemy[idEnemigo] = (false, proximoNodoEnemy[idEnemigo].Item2, proximoNodoEnemy[idEnemigo].Item3);
        if (velocidad > 0)
        {
            rb.linearVelocity = new Vector2(velocityInGround, 0);
        }
        else {
            rb.linearVelocity = new Vector2(-velocityInGround, 0);
        }
        Debug.Log("escalaEnemigo " + enemigo.transform.localScale.x / 2);
        Debug.Log("marge " + (margenDesdeElBorde )); 
        Debug.Log("se va a esperar "+ ((margenDesdeElBorde + enemigo.transform.localScale.x / 2) / velocityInGround));
            yield return new WaitForSeconds((margenDesdeElBorde  + enemigo.transform.localScale.x/2)/ velocityInGround);
        rb.linearVelocity = new Vector2(velocidad, rb.linearVelocity.y);
        yield return new WaitForSeconds(tiempoCaida-0.05f); 
        actualizarPiso[idEnemigo] = true;
    }




    private CuatroEnteros obtenerCuatroEnteros(int nodoInicial, int nodoFinal) {
        List<CuatroEnteros> lista = mapa[nodoInicial];

        foreach (CuatroEnteros datos in lista) {
            if (datos.a == nodoFinal) { 
                return datos;
            }
        }
        return new CuatroEnteros(-1,-1,-1,-1);
    }

    public void setProximoNodoEnemyPiso(int idEnemigo, bool piso) {
        if (actualizarPiso[idEnemigo])
        {
            if (!proximoNodoEnemy.ContainsKey(idEnemigo))
            {
                proximoNodoEnemy[idEnemigo] = (piso, -1, null);
            }
            else
            {
                proximoNodoEnemy[idEnemigo] = (piso, proximoNodoEnemy[idEnemigo].Item2, proximoNodoEnemy[idEnemigo].Item3);
            }
        }
    }

    private void dibujarLineas() {

        foreach (var par in mapa)
        {
            int idOrigen = par.Key;
            GameObject nodoOrigen = GameObject.Find("Nodo" + idOrigen);
            if (nodoOrigen == null) continue;

            Vector3 origenPos = nodoOrigen.transform.position;

            foreach (var conexion in par.Value)
            {
                int idDestino = conexion.a;
                int tipo = conexion.b;

                GameObject nodoDestino = GameObject.Find("Nodo" + idDestino);
                if (nodoDestino == null) continue;

                Vector3 destinoPos = nodoDestino.transform.position;

                Color colorLinea = Color.black;
                switch (tipo)
                {
                    case 0: colorLinea = Color.blue; break;
                    case 1: colorLinea = Color.white; break;
                    case 2: colorLinea = Color.black; break;
                }
                if (tipo == 2)
                {
                    Debug.DrawLine(new Vector3(origenPos.x + 0.15f, origenPos.y, origenPos.z), new Vector3(destinoPos.x + 0.15f, destinoPos.y, destinoPos.z), colorLinea, 4f);
                }

                else
                {
                    Debug.DrawLine(origenPos, destinoPos, colorLinea, 4f);
                }
            }
        }
    }



    void CrearNodoEnPlataformaDebajo(GameObject nodoOrigen, int derecha)
    {
        Vector2 origen = Vector2.zero;
        float margen = margenDesdeElBorde * 6;
        if (derecha == 0)

        {
            origen = new Vector2(nodoOrigen.transform.position.x + margenDesdeElBorde + margen, nodoOrigen.transform.position.y);
        }
        else {
            origen = new Vector2(nodoOrigen.transform.position.x -(margenDesdeElBorde+ margen), nodoOrigen.transform.position.y);
        }
        float distanciaMaxima = 30f; // ESTO PUEDE DAR ERROR SI ES MUY POCO !!!!!!

        RaycastHit2D hit = Physics2D.Raycast(origen, Vector2.down, distanciaMaxima);


        if (hit.collider != null && hit.collider.CompareTag("plataform"))
        {
            Vector2 puntoImpacto = hit.point;
            Transform plataforma = hit.collider.transform;

            // Ajustamos un poco la altura para que no esté "pegado" al collider
            Vector3 posicionNodo = new Vector3(puntoImpacto.x, puntoImpacto.y, nodoOrigen.transform.position.z);



            Transform nodoDerecha = null;
            Transform nodoIzquierda = null;
            float distanciaMinDerecha = Mathf.Infinity;
            float distanciaMinIzquierda = Mathf.Infinity;

            // 4. Recorremos los nodos existentes
            foreach (Transform nodo in plataforma)
            {

                float x = nodo.position.x;
                float dx = x - puntoImpacto.x;

                if (dx > 0 && dx < distanciaMinDerecha)
                {
                    distanciaMinDerecha = dx;
                    nodoDerecha = nodo;
                }
                else if (dx < 0 && -dx < distanciaMinIzquierda)
                {
                    distanciaMinIzquierda = -dx;
                    nodoIzquierda = nodo;
                }
            }

            // 5. Obtener los IDs desde el nombre del objeto
            int idDerecha = -1;
            int idIzquierda = -1;
            int idNodoOrigen = -1;

            if (nodoDerecha != null)
            {
                string nombre = nodoDerecha.name; // "NodoX"
                if (nombre.StartsWith("Nodo"))
                    int.TryParse(nombre.Substring(4), out idDerecha);
            }
            else
            {

                Debug.Log("ERRORRRRRRR DERECHA");
            }

            if (nodoIzquierda != null)
            {
                string nombre = nodoIzquierda.name; // "NodoX"
                if (nombre.StartsWith("Nodo"))
                    int.TryParse(nombre.Substring(4), out idIzquierda);
            }
            else {

                Debug.Log("ERRORRRRRRR IZQUIERDA");
            }

            if (nodoOrigen != null)
            {
                string nombre = nodoOrigen.name; // "NodoX"
                if (nombre.StartsWith("Nodo"))
                    int.TryParse(nombre.Substring(4), out idNodoOrigen);
            }
            else
            {

                Debug.Log("ERRORRRRRRR nodo origen");
                Debug.Log("ERRORRRRRRR nodo origen");
                Debug.Log("ERRORRRRRRR nodo origen");
                Debug.Log("e");
                Debug.Log("e");
                Debug.Log("e");
                Debug.Log("e");
                Debug.Log("e");
                Debug.Log("e");
            }




            GameObject nuevoNodo = CrearNodoHijo(plataforma, posicionNodo);
            float tiempoCaidaCuadrado = (nodoOrigen.transform.position.y - posicionNodo.y) * 2 / 9.81f;
            float tiempoCaida = Mathf.Sqrt(tiempoCaidaCuadrado);
            float velocidadHorizontal = 0;
            if (derecha == 0)
            {
                velocidadHorizontal = margen / tiempoCaida;
            }
            else {
                velocidadHorizontal = - margen / tiempoCaida;
            }
            AgregarElemento(idNodoOrigen, new CuatroEnteros(contadorNodos - 1, 2, velocidadHorizontal, tiempoCaida));

            Debug.Log("Nodo creado debajo de " + nodoOrigen.name + " en " + posicionNodo);

            AgregarElemento(contadorNodos - 1, new CuatroEnteros(idDerecha, 0, 0, distanciaMinDerecha / velocityInGround));
            AgregarElemento(contadorNodos - 1, new CuatroEnteros(idIzquierda, 0, 1, distanciaMinIzquierda / velocityInGround));

            if (mapa.ContainsKey(idIzquierda))
            {
                List<CuatroEnteros> lista = mapa[idIzquierda];

                // Buscar y eliminar el CuatroEnteros con .a == idDerecha
                lista.RemoveAll(elem => elem.a == idDerecha);
                CuatroEnteros nuevo = new CuatroEnteros(contadorNodos - 1, 0, 0, distanciaMinIzquierda / velocityInGround);
                lista.Add(nuevo);
            }
            if (mapa.ContainsKey(idDerecha))
            {
                List<CuatroEnteros> lista = mapa[idDerecha];

                // Buscar y eliminar el CuatroEnteros con .a == idDerecha
                lista.RemoveAll(elem => elem.a == idIzquierda);
                CuatroEnteros nuevo = new CuatroEnteros(contadorNodos - 1, 0, 1, distanciaMinDerecha / velocityInGround);
                lista.Add(nuevo);
            }

        }
    }

    private void actualizarCaminosPlataforma(GameObject nuevoNodo, GameObject plataforma)
    {

        Vector3 posicion = new Vector3(nuevoNodo.transform.position.x, nuevoNodo.transform.position.y, nuevoNodo.transform.position.z);

        Transform nodoDerecha = null;
        Transform nodoIzquierda = null;
        float distanciaMinDerecha = Mathf.Infinity;
        float distanciaMinIzquierda = Mathf.Infinity;

        // 4. Recorremos los nodos existentes
        foreach (Transform nodo in plataforma.transform)
        {

            float x = nodo.position.x;
            float dx = x - posicion.x;

            if (dx > 0 && dx < distanciaMinDerecha)
            {
                distanciaMinDerecha = dx;
                nodoDerecha = nodo;
            }
            else if (dx < 0 && -dx < distanciaMinIzquierda)
            {
                distanciaMinIzquierda = -dx;
                nodoIzquierda = nodo;
            }
        }

        // 5. Obtener los IDs desde el nombre del objeto
        int idDerecha = -1;
        int idIzquierda = -1;
        int idNodoOrigen = -1;

        if (nodoDerecha != null)
        {
            string nombre = nodoDerecha.name; // "NodoX"
            if (nombre.StartsWith("Nodo"))
                int.TryParse(nombre.Substring(4), out idDerecha);
        }
        else
        {

            Debug.Log("ERRORRRRRRR DERECHA");
        }

        if (nodoIzquierda != null)
        {
            string nombre = nodoIzquierda.name; // "NodoX"
            if (nombre.StartsWith("Nodo"))
                int.TryParse(nombre.Substring(4), out idIzquierda);
        }
        else
        {

            Debug.Log("ERRORRRRRRR IZQUIERDA");
        }

        if (nuevoNodo != null)
        {
            string nombre = nuevoNodo.name; // "NodoX"
            if (nombre.StartsWith("Nodo"))
                int.TryParse(nombre.Substring(4), out idNodoOrigen);
        }
        else
        {

            Debug.Log("ERRORRRRRRR nodo origen");

        }



        Debug.Log("se crea camino desde "+ (contadorNodos - 1) +"hasta"+ idDerecha);
        AgregarElemento(contadorNodos - 1, new CuatroEnteros(idDerecha, 0, 0, distanciaMinDerecha / velocityInGround));
        Debug.Log("se crea camino desde " + (contadorNodos - 1) + "hasta" + idIzquierda);
        AgregarElemento(contadorNodos - 1, new CuatroEnteros(idIzquierda, 0, 1, distanciaMinIzquierda / velocityInGround));

        if (mapa.ContainsKey(idIzquierda))
        {
            List<CuatroEnteros> lista = mapa[idIzquierda];

            // Buscar y eliminar el CuatroEnteros con .a == idDerecha
            lista.RemoveAll(elem => elem.a == idDerecha);
            CuatroEnteros nuevo = new CuatroEnteros(contadorNodos - 1, 0, 0, distanciaMinIzquierda / velocityInGround);
            Debug.Log("se crea camino desde " + (idIzquierda) + "hasta" + (contadorNodos - 1));
            lista.Add(nuevo);
        }
        if (mapa.ContainsKey(idDerecha))
        {
            List<CuatroEnteros> lista = mapa[idDerecha];

            // Buscar y eliminar el CuatroEnteros con .a == idDerecha
            lista.RemoveAll(elem => elem.a == idIzquierda);
            CuatroEnteros nuevo = new CuatroEnteros(contadorNodos - 1, 0, 1, distanciaMinDerecha / velocityInGround);
            Debug.Log("se crea camino desde " + (idDerecha) + "hasta" + (contadorNodos - 1));
            lista.Add(nuevo);
        }
    }


private void imprimirCamino(int inicial, int fianl)
    {
        List<int> lista = AStar(inicial, fianl);

        foreach (int i in lista)
        {
            Debug.Log(i);
        }
    }
    GameObject CrearNodoHijo(Transform padre, Vector3 posicion)
    {
        GameObject nodo = new GameObject("Nodo" + contadorNodos++);
        nodo.transform.parent = padre;
        nodo.transform.position = posicion;
        nodo.tag = "Nodo";
        posicionesNodos[contadorNodos - 1] = new Vector2(nodo.transform.position.x, nodo.transform.position.y);
        return nodo;
    }

    void AgregarElemento(int clave, CuatroEnteros nuevo)
    {
        if (!mapa.ContainsKey(clave))
        {
            mapa[clave] = new List<CuatroEnteros>();
        }
        foreach (CuatroEnteros ce in mapa[clave])
        {
            if (ce.a == nuevo.a && ce.d <= nuevo.d)
            {
                return; //no agregamos porque ya hay uno mejor
            }
        }

        mapa[clave].Add(nuevo);
    }

    (bool posible, float velX, float tiempo) EsSaltoPosibleYVelocidad(Vector2 origen, Vector2 destino)
    {
        float g = 9.81f;
        float y0 = origen.y;
        float y1 = destino.y;
        float dy = y1 - y0;
        Vector2 vectorMaxHeight = Vector2.zero;

        // velocidad vertical inicial de salto
        float v0y = velocityJump;

        // Resolver la ecuación: y1 = y0 + v0y*t - 0.5*g*t^2
        // => 0.5*g*t^2 - v0y*t + dy = 0
        float a = 0.5f * g;
        float b = -v0y;
        float c = dy;

        float discriminante = b * b - 4 * a * c;

        if (discriminante < 0)
        {
            // No hay solución real: el salto no llega a esa altura
            return (false, 0f, 0f);
        }

        // t es el tiempo en el que se llega a la altura final y la velocidad vertical ya es descendente
        float sqrtDiscriminante = Mathf.Sqrt(discriminante);
        float t1 = (-b - sqrtDiscriminante) / (2 * a);
        float t2 = (-b + sqrtDiscriminante) / (2 * a);

        // Elegimos la t que representa cuando el personaje ya está cayendo (v < 0)
        float t = Mathf.Max(t1, t2);
        if (t < 0) return (false, 0f, 0f);

        // Verificamos que esté cayendo en ese momento (velocidad vertical < 0)
        float velY = v0y - g * t;
        if (velY > 0) return (false, 0f, 0f); // aún está subiendo

        // Calcular velocidad horizontal necesaria
        float dx = destino.x - origen.x;
        float velX = dx / t;

        if (Mathf.Abs(velX) > maxVelocityInAir)
        {
            // No puede alcanzar esa distancia horizontal en el aire
            return (false, 0f, 0f);
        }

        float tiempoLLegarArriba = v0y / g;

        vectorMaxHeight = new Vector2(origen.x + velX * tiempoLLegarArriba, origen.y + v0y * tiempoLLegarArriba - g * tiempoLLegarArriba * tiempoLLegarArriba / 2);

        bool returneable = HayObstaculoEntre(origen, vectorMaxHeight, destino);

        return (!returneable, velX, t);
    }

    bool HayObstaculoEntre(Vector2 origen, Vector2 vectorMaxHeight, Vector2 destino)
    {
        Vector2 origenAjustado = origen + Vector2.up * 0.1f;
        Vector2 destinoAjustado = destino + Vector2.up * 0.1f;
        Vector2 maxHeightAjustado = vectorMaxHeight + Vector2.up * 0.1f;

        // Rayos
        Vector2 dir1 = (maxHeightAjustado - origenAjustado).normalized;
        float dist1 = Vector2.Distance(origenAjustado, maxHeightAjustado);

        Vector2 dir2 = (destinoAjustado - maxHeightAjustado).normalized;
        float dist2 = Vector2.Distance(maxHeightAjustado, destinoAjustado);

        // RaycastAll desde origen a punto más alto
        RaycastHit2D[] hits1 = Physics2D.RaycastAll(origenAjustado, dir1, dist1);
        RaycastHit2D[] hits2 = Physics2D.RaycastAll(maxHeightAjustado, dir2, dist2);


        return HayObstaculo(hits1, origen, destino) || HayObstaculo(hits2, origen, destino);
    }


    // Función auxiliar para detectar si hay obstáculos válidos
    bool HayObstaculo(RaycastHit2D[] hits, Vector2 origen, Vector2 destino)
    {
        foreach (var hit in hits)
        {
            if (hit.collider != null && (hit.collider.CompareTag("obstacle") || hit.collider.CompareTag("plataform")))
            {
                // Asegurarse de que no sea el origen o el destino mismo
                if (!EsPlataformaDeOrigenODestino(hit.collider.transform, origen, destino))//esto es un ERROR, HAY QUE CHEQUEAR LAS PLATAFORMAS ORIGEN Y DESTINO TAMBIEN 
                    return true;
            }
        }
        return false;
    }

    bool EsPlataformaDeOrigenODestino(Transform col, Vector2 origen, Vector2 destino)
    {
        Vector2 pos = col.position;
        float tolerancia = 0.5f; // margen para considerar la plataforma misma
        return (Vector2.Distance(pos, origen) < tolerancia) || (Vector2.Distance(pos, destino) < tolerancia);
    }

    [System.Serializable]
    public struct CuatroEnteros
    {
        public int a;
        public int b;
        public float c;
        public float d;

        public CuatroEnteros(int a, int b, float c, float d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    }





    public List<int> AStar(int inicio, int fin)
    {
        HashSet<int> visitados = new HashSet<int>();
        PriorityQueue<NodoAStar> abierta = new PriorityQueue<NodoAStar>();

        abierta.Enqueue(new NodoAStar(inicio, 0f, Heuristica(inicio, fin), -1));

        Dictionary<int, float> costos = new Dictionary<int, float>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        costos[inicio] = 0f;

        while (abierta.Count > 0)
        {
            NodoAStar actual = abierta.Dequeue();

            if (actual.id == fin)
            {
                return ReconstruirCamino(cameFrom, actual.id);
            }

            if (visitados.Contains(actual.id)) continue;
            visitados.Add(actual.id);

            if (!mapa.ContainsKey(actual.id)) continue;

            foreach (var vecino in mapa[actual.id])
            {
                int vecinoId = vecino.a;
                float tiempo = vecino.d;

                float nuevoCosto = costos[actual.id] + tiempo;

                if (!costos.ContainsKey(vecinoId) || nuevoCosto < costos[vecinoId])
                {
                    costos[vecinoId] = nuevoCosto;
                    float prioridad = nuevoCosto + Heuristica(vecinoId, fin);
                    abierta.Enqueue(new NodoAStar(vecinoId, nuevoCosto, prioridad, actual.id));
                    cameFrom[vecinoId] = actual.id;
                }
            }
        }

        return new List<int>();
    }

    float Heuristica(int a, int b)
    {
        Vector2 posA = posicionesNodos[a];
        Vector2 posB = posicionesNodos[b];
        return Vector2.Distance(posA, posB);
    }

    List<int> ReconstruirCamino(Dictionary<int, int> cameFrom, int nodoFinal)
    {
        List<int> camino = new List<int>();
        int actual = nodoFinal;
        while (cameFrom.ContainsKey(actual))
        {
            camino.Add(actual);
            actual = cameFrom[actual];
        }
        camino.Add(actual); 
        camino.Reverse();
        return camino;
    }

    struct NodoAStar : System.IComparable<NodoAStar>
    {
        public int id;
        public float g; // costo acumulado
        public float f; // costo total estimado
        public int padre;

        public NodoAStar(int id, float g, float f, int padre)
        {
            this.id = id;
            this.g = g;
            this.f = f;
            this.padre = padre;
        }

        public int CompareTo(NodoAStar other)
        {
            return this.f.CompareTo(other.f);
        }
    }

    public class PriorityQueue<T> where T : System.IComparable<T>
    {
        List<T> datos = new List<T>();

        public void Enqueue(T item)
        {
            datos.Add(item);
            datos.Sort(); // aca lo ordena por f porque el comperto se hace por el f
        }

        public T Dequeue()
        {
            T item = datos[0];
            datos.RemoveAt(0);
            return item;
        }

        public int Count => datos.Count;
    }

    public void actualizarPuntoContactoEnemigo(int clave, Vector2 nuevoPunto, GameObject plataforma)
    {
        if (actualizarPiso[clave])
        {
            puntoContactoEnemigo[clave] = (nuevoPunto, plataforma);
        }
    }

    public void setPuntoContactoPlayer( Vector2 Vector, GameObject plataforma) {
        if (puntoContactoPlayer.Item2 !=null &&  plataforma != puntoContactoPlayer.Item2) {
            actualizarCaminos = true;
        }
        puntoContactoPlayer = (Vector , plataforma);
    }


    Dictionary<int, List<CuatroEnteros>> CopiarMapa(Dictionary<int, List<CuatroEnteros>> original)
    {
        Dictionary<int, List<CuatroEnteros>> copia = new Dictionary<int, List<CuatroEnteros>>();

        foreach (var kvp in original)
        {
            int key = kvp.Key;
            List<CuatroEnteros> listaOriginal = kvp.Value;

            List<CuatroEnteros> listaCopia = new List<CuatroEnteros>();
            foreach (CuatroEnteros item in listaOriginal)
            {
                CuatroEnteros copiaCuatroEnteros = new CuatroEnteros(item.a,item.b,item.c,item.d);
                listaCopia.Add(copiaCuatroEnteros); // se copia por valor porque CuatroEnteros es un struct
            }

            copia[key] = listaCopia;
        }

        return copia;
    }

}
