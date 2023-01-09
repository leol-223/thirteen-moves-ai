using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public enum Player {Blue, Red, Neutral}

    public Player player;
    public Color color;
    public GameObject gameObject_;
    private bool removed = false;

    public Tile(float[] position, GameObject tilePrefab, Player player_, Color color_, int layer=0) {
        player = player_;
        color = color_;

        gameObject_ = Object.Instantiate(tilePrefab, new Vector2(position[0], position[1]), Quaternion.identity);
        gameObject_.transform.localScale = new Vector2(position[2], position[2]);
        gameObject_.GetComponent<SpriteRenderer>().color = color_;
        gameObject_.GetComponent<SpriteRenderer>().sortingOrder = layer;
    }

    public void setColor(Color color) {
        gameObject_.GetComponent<SpriteRenderer>().color = color;
    }

    public void Destroy() {
        Object.Destroy(gameObject_);
        removed = true;
    }

    public void setPosition(float x, float y) {
        if (!removed) {
            gameObject_.transform.position = new Vector2(x, y);
        }
    }
}
