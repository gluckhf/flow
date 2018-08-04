using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScript : MonoBehaviour {

    public MeshRenderer mesh_renderer;
    public Material disco_material;
    RenderTexture render_texture;
    RenderTexture disco_texture;
    Texture2D initial_data;

    int size = 128;

	// Use this for initialization
	void Start () {
        // Set up textures for use
        render_texture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        disco_texture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        mesh_renderer.material.SetTexture("_MainTex", render_texture);

        // Initialize color to red
        initial_data = new Texture2D(size, size);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                initial_data.SetPixel(x, y, Color.red);
            }
        }
        initial_data.Apply();
        Graphics.Blit(initial_data, render_texture);

    }
	
	// Update is called once per frame
	void Update () {
        Graphics.Blit(render_texture, disco_texture, disco_material);
        Graphics.Blit(disco_texture, render_texture);
    }
}
