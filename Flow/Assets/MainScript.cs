using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScript : MonoBehaviour {

    public MeshRenderer mesh_renderer;
    RenderTexture render_texture;
    Texture2D initial_data;

    // Required for using the water shader
    public Material water_material;
    RenderTexture water_texture;
    

    int width = 128;
    int height = 64;

	// Use this for initialization
	void Start () {

        transform.localScale = new Vector3(1f, (float)height / (float)width, 1f);

        // Set up textures for use
        render_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        water_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        mesh_renderer.material.SetTexture("_MainTex", render_texture);

        // Textures must be set to point filter
        render_texture.filterMode = FilterMode.Point;
        water_texture.filterMode = FilterMode.Point;

        // Set the sizes of the material
        water_material.SetFloat("_TexelWidth", 1.0f / width);
        water_material.SetFloat("_TexelHeight", 1.0f / height);

        render_texture.wrapMode = TextureWrapMode.Repeat;

        // Initialize color to red
        initial_data = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                initial_data.SetPixel(x, y, new Color(Random.Range(0f,1f),0,0));
            }
        }
        initial_data.Apply();
        Graphics.Blit(initial_data, render_texture);

    }
	
	// Update is called once per frame
	void Update () {
        Graphics.Blit(render_texture, water_texture, water_material);
        Graphics.Blit(water_texture, render_texture);
    }
}
