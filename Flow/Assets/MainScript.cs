using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainScript : MonoBehaviour {

    public MeshRenderer mesh_renderer;
    RenderTexture render_texture;

    // World compositing
    public Material world_material;
    private Material[] world_material_flip_flops = new Material[2];

    // Water components
    public Material water_material;
    RenderTexture[] water_texture = new RenderTexture[2];

    // Dirt components
    public Material dirt_material;
    RenderTexture[] dirt_texture = new RenderTexture[2];

    // Provides a link to the debug text
    public Text DebugText;
    
    int width = 128;
    int height = 64;

    int flip_flop = 0;

    // Use this for initialization
    void Start() {

        transform.localScale = new Vector3(1f, (float)height / (float)width, 1f);
        
        // Set up the main render texture
        render_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        render_texture.useMipMap = false;
        render_texture.autoGenerateMips = false;
        render_texture.wrapMode = TextureWrapMode.Repeat;
        render_texture.filterMode = FilterMode.Point;
        
        // Set up the element textures
        // TODO: Element bank with double indexing
        for (int i = 0; i < 2; i++)
        {
            water_texture[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            water_texture[i].useMipMap = false;
            water_texture[i].autoGenerateMips = false;
            water_texture[i].filterMode = FilterMode.Point;

            dirt_texture[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            dirt_texture[i].useMipMap = false;
            dirt_texture[i].autoGenerateMips = false;
            dirt_texture[i].filterMode = FilterMode.Point;
        }

        // Set up the mesh renderer
        mesh_renderer.material.SetTexture("_MainTex", render_texture);
        
        // Set the sizes of the element materials
        water_material.SetFloat("_TexelWidth", 1.0f / width);
        water_material.SetFloat("_TexelHeight", 1.0f / height);

        dirt_material.SetFloat("_TexelWidth", 1.0f / width);
        dirt_material.SetFloat("_TexelHeight", 1.0f / height);

        // Initialize water to random
        {
            Texture2D initial_data = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    initial_data.SetPixel(x, y, new Color(0, 0, 0));// Random.Range(0f,1f),0,0));
                }
            }
            initial_data.Apply();
            Graphics.Blit(initial_data, water_texture[0]);
            Graphics.Blit(initial_data, water_texture[1]);
        }

        // Initialize dirt to boxes
        {
            Texture2D initial_data = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y == 16 || y == 48 || x == 32 || x == 96)
                    {
                        initial_data.SetPixel(x, y, new Color(1, 0, 0));
                    }
                    else
                    {
                        initial_data.SetPixel(x, y, new Color(0, 0, 0));
                    }
                }
            }
            initial_data.Apply();
            Graphics.Blit(initial_data, dirt_texture[0]);
            Graphics.Blit(initial_data, dirt_texture[1]);
        }

        // Set up the world components which point to the now-initialized textures
        for (int i = 0; i < 2; i++)
        {
            world_material_flip_flops[i] = new Material(world_material);
            world_material_flip_flops[i].SetFloat("_TexelWidth", 1.0f / width);
            world_material_flip_flops[i].SetFloat("_TexelHeight", 1.0f / height);
            world_material_flip_flops[i].SetTexture("_WaterTex", water_texture[i]);
            world_material_flip_flops[i].SetTexture("_DirtTex", dirt_texture[i]);
        }
    }
	
	// Update is called once per frame
	/// <summary>
    /// 
    /// </summary>
    void Update () {

        // Convert mouse position to Grid Coordinates
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 local_pos = transform.InverseTransformPoint(pos);
        Vector3 texture_pos = local_pos + Vector3.one * 0.5f;
        Vector2Int pos_new = new Vector2Int((int)(texture_pos.x * width), (int)(texture_pos.y * height));

        DebugText.text = pos.ToString() + "\n" + local_pos.ToString() + "\n" + texture_pos.ToString() + "\n" + pos_new.ToString();
        
        // On mouse click left
        if (Input.GetMouseButton(0))
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            // Set the supplied RenderTexture as the active one
            RenderTexture.active = water_texture[flip_flop];

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(water_texture[flip_flop].width, water_texture[flip_flop].height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.SetPixel(pos_new.x, pos_new.y, Color.red);
            tex.Apply();

            // TODO: Hack? This line shouldn't be required but somehow needs to be here.
            // Also the "proper" way to do this would be to write a shader to modify the texture then run the shader
            Graphics.Blit(tex, water_texture[flip_flop]);

            // Restore previously active render texture
            RenderTexture.active = currentActiveRT;
        }

        // On mouse click right
        if (Input.GetMouseButton(1))
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            // Set the supplied RenderTexture as the active one
            RenderTexture.active = dirt_texture[flip_flop];

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(dirt_texture[flip_flop].width, dirt_texture[flip_flop].height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.SetPixel(pos_new.x, pos_new.y, Color.red);
            tex.Apply();

            // TODO: Hack? This line shouldn't be required but somehow needs to be here.
            // Also the "proper" way to do this would be to write a shader to modify the texture then run the shader
            Graphics.Blit(tex, dirt_texture[flip_flop]);

            // Restore previously active render texture
            RenderTexture.active = currentActiveRT;
        }

        // Flip the buffer
        flip_flop = 1 - flip_flop;

        // Run the shaders
        Graphics.Blit(water_texture[1 - flip_flop], water_texture[flip_flop], water_material);
        Graphics.Blit(dirt_texture[1 - flip_flop], dirt_texture[flip_flop], dirt_material);

        // Use Blit to run the world shader (which references the water and dirt textures) and save the result to render_texture
        Graphics.Blit(null, render_texture, world_material_flip_flops[flip_flop]);
    }
}
