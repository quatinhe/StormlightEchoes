using UnityEngine;


public class NoHeadJump : MonoBehaviour
{
    void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                Rigidbody2D rigidBody = collision.gameObject.GetComponent<Rigidbody2D>();
                if (rigidBody != null)
                {
                    float relativePosition = collision.transform.position.x - transform.position.x;
                    float direction = Mathf.Sign(relativePosition);

                    rigidBody.AddForce(new Vector2(500f * direction, 0f));
                }

                break;
            }
        }
    }
}