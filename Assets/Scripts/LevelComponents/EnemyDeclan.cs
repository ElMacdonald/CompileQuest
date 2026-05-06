using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyDeclan : MonoBehaviour
{
    public shipMovement shipMovement;
    public GameObject winPanel;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag != "Projectile") return;

        if (shipMovement.power == 10)
        {
            winPanel.SetActive(true);
            SoundManager.Play("victory");
            Destroy(other.gameObject);

            if (Session.currentPlayer != null)
                Session.currentPlayer.coins += 10;

            if (LevelManager.Instance != null)
                LevelManager.Instance.CompleteLevel(SceneManager.GetActiveScene().buildIndex);

            Destroy(this.gameObject);
        }
        else
        {
            Destroy(other.gameObject);
        }
    }
}
