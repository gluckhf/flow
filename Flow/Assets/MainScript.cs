using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainScript : MonoBehaviour {

    public MeshRenderer mesh_renderer;
    
    // Height map
    public Material height_material;
    private Material[] height_material_flip_flops = new Material[2];
    RenderTexture height_texture;

    // World compositing
    public Material world_material;
    private Material[] world_material_flip_flops = new Material[2];
    RenderTexture render_texture;

    // Water components
    public Material water_material;
    RenderTexture[] water_texture = new RenderTexture[2];

    // Mud components
    public Material mud_material;
    RenderTexture[] mud_texture = new RenderTexture[2];

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
        render_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        render_texture.useMipMap = false;
        render_texture.autoGenerateMips = false;
        render_texture.wrapMode = TextureWrapMode.Repeat;
        render_texture.filterMode = FilterMode.Point;

        height_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        height_texture.useMipMap = false;
        height_texture.autoGenerateMips = false;
        height_texture.wrapMode = TextureWrapMode.Repeat;
        height_texture.filterMode = FilterMode.Point;
        
        // Set up the element textures
        // TODO: Element bank with double indexing
        int num_elements = 0;
        for (int i = 0; i < 2; i++)
        {
            water_texture[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            water_texture[i].useMipMap = false;
            water_texture[i].autoGenerateMips = false;
            water_texture[i].filterMode = FilterMode.Point;
            num_elements++;

            mud_texture[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            mud_texture[i].useMipMap = false;
            mud_texture[i].autoGenerateMips = false;
            mud_texture[i].filterMode = FilterMode.Point;
            num_elements++;

            dirt_texture[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            dirt_texture[i].useMipMap = false;
            dirt_texture[i].autoGenerateMips = false;
            dirt_texture[i].filterMode = FilterMode.Point;
            num_elements++;
        }

        // Set up the mesh renderer
        mesh_renderer.material.SetTexture("_MainTex", render_texture);
        
        // Set the sizes of the element materials
        water_material.SetFloat("_TexelWidth", 1.0f / width);
        water_material.SetFloat("_TexelHeight", 1.0f / height);
        water_material.SetFloat("_NumElements", num_elements);

        mud_material.SetFloat("_TexelWidth", 1.0f / width);
        mud_material.SetFloat("_TexelHeight", 1.0f / height);
        mud_material.SetFloat("_NumElements", num_elements);

        dirt_material.SetFloat("_TexelWidth", 1.0f / width);
        dirt_material.SetFloat("_TexelHeight", 1.0f / height);
        dirt_material.SetFloat("_NumElements", num_elements);

        // Initialize water
        {
            Texture2D initial_data = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //initial_data.SetPixel(x, y, new Color(Random.Range(0f,1f),0,0));
                    initial_data.SetPixel(x, y, new Color(0, 0, 0));
                }
            }
            initial_data.Apply();
            Graphics.Blit(initial_data, water_texture[0]);
            Graphics.Blit(initial_data, water_texture[1]);
        }

        // Initialize mud
        {
            Texture2D initial_data = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //initial_data.SetPixel(x, y, new Color(Random.Range(0f, 1f), 0, 0));
                    initial_data.SetPixel(x, y, new Color(0, 0, 0));
                }
            }
            initial_data.Apply();
            Graphics.Blit(initial_data, mud_texture[0]);
            Graphics.Blit(initial_data, mud_texture[1]);
        }

        // Initialize dirt to boxes
        {
            Texture2D initial_data = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y == 0 || y == 16 || y == height-1 || x == 0 || x == 16 || x == width-1)
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
            world_material_flip_flops[i].SetFloat("_NumElements", num_elements);
            world_material_flip_flops[i].SetTexture("_WaterTex", water_texture[i]);
            world_material_flip_flops[i].SetTexture("_MudTex", mud_texture[i]);
            world_material_flip_flops[i].SetTexture("_DirtTex", dirt_texture[i]);
        }

        // Set up the height map components which point to the now-initialized textures
        for (int i = 0; i < 2; i++)
        {
            height_material_flip_flops[i] = new Material(height_material);
            height_material_flip_flops[i].SetFloat("_TexelWidth", 1.0f / width);
            height_material_flip_flops[i].SetFloat("_TexelHeight", 1.0f / height);
            height_material_flip_flops[i].SetFloat("_NumElements", num_elements);
            height_material_flip_flops[i].SetTexture("_WaterTex", water_texture[i]);
            height_material_flip_flops[i].SetTexture("_MudTex", mud_texture[i]);
            height_material_flip_flops[i].SetTexture("_DirtTex", dirt_texture[i]);
        }

        // Set up the elements to reference the height maps
        dirt_material.SetTexture("_HeightTex", height_texture);
        mud_material.SetTexture("_HeightTex", height_texture);
        water_material.SetTexture("_HeightTex", height_texture);

        // Write initial height maps
        for (int i = 0; i < 2; i++)
        {
            Graphics.Blit(null, height_texture, height_material_flip_flops[i]);
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
        Vector2Int pos_grid = new Vector2Int((int)(texture_pos.x * width), (int)(texture_pos.y * height));

        DebugText.text = pos_grid.ToString();

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
            
            for (int i = -1; i < 1; i++)
            {
                for (int j = -1; j < 1; j++)
                {
                    tex.SetPixel(pos_grid.x + i, pos_grid.y + j, Color.red);  
                }
            }

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
            RenderTexture.active = mud_texture[flip_flop];

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(mud_texture[flip_flop].width, mud_texture[flip_flop].height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.SetPixel(pos_grid.x, pos_grid.y, Color.red);
            tex.Apply();

            // TODO: Hack? This line shouldn't be required but somehow needs to be here.
            // Also the "proper" way to do this would be to write a shader to modify the texture then run the shader
            Graphics.Blit(tex, mud_texture[flip_flop]);

            // Restore previously active render texture
            RenderTexture.active = currentActiveRT;
        }

        // Flip the buffer
        flip_flop = 1 - flip_flop;
        
        // Run the shaders for flowable elements
        Graphics.Blit(water_texture[1 - flip_flop], water_texture[flip_flop], water_material);
        Graphics.Blit(mud_texture[1 - flip_flop], mud_texture[flip_flop], mud_material);

        // Dirt doesn't flow so it doesn't need a shader, but it's here for completeness and maybe use later
        Graphics.Blit(dirt_texture[1 - flip_flop], dirt_texture[flip_flop], dirt_material);

        // Use Blit to run the world shader (which references the water and dirt textures) and save the result to render_texture
        Graphics.Blit(null, render_texture, world_material_flip_flops[flip_flop]);

        // Write the new height maps for use next frame
        Graphics.Blit(null, height_texture, height_material_flip_flops[flip_flop]);
    }
}
