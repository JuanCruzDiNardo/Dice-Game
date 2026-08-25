using UnityEngine;

public class EnemyVisualTestController : MonoBehaviour
{
    [SerializeField]
    private EnemyVisualController enemyVisual;

    private void OnGUI()
    {
        if (!enemyVisual)
            return;

        const float width = 180f;
        const float height = 40f;
        const float spacing = 10f;

        float x = 20f;
        float y = 20f;

        if (GUI.Button(
            new Rect(x, y, width, height),
            "Walking"))
        {
            enemyVisual.PlayWalk();
        }

        y += height + spacing;

        if (GUI.Button(
            new Rect(x, y, width, height),
            "Death"))
        {
            enemyVisual.PlayDeath();
        }

        if (!enemyVisual)
            return;

        y += height + spacing;

        GUI.Label(
            new Rect(x, y, width, height),
            $"State: {enemyVisual.CurrentState}"
        );
    }
}