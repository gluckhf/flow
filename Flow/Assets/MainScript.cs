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
    RenderTexture world_texture;

    // Flow components
    private enum flows
    {
        water = 0,
        mud,
        size
    }
    
    public Material flow_material;
    private Material[] flow_materials = new Material[(int)flows.size];
    RenderTexture[,] flow_textures = new RenderTexture[2, (int)flows.size];

    // Solid components
    private enum solids
    {
        dirt = 0,
        size
    }
    
    RenderTexture[,] solid_textures = new RenderTexture[2, (int)solids.size];
    
    // Provides a link to the debug text
    public Text DebugText;

    // World size - powers of 2 for optimal efficiency
    public int width = 128;
    public int height = 64;

    // A-B rendering buffer selection
    int flip_flop = 0;

    // Update rate (per second) - independent of framerate
    [Range(500f, 2000f)]
    public float update_rate = 1000f;

    // Use this for initialization
    void Start() {
        // Match the local scale to the scale set up in the input parameters
        transform.localScale = new Vector3(1f, (float)height / (float)width, 1f);

        height_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        height_texture.useMipMap = false;
        height_texture.autoGenerateMips = false;
        height_texture.wrapMode = TextureWrapMode.Repeat;
        height_texture.filterMode = FilterMode.Point;
        
        // Flowables
        {
            // Set up flowable common attributes with default "hovering" gradient
            for (int mat = 0; mat < (int)flows.size; mat++)
            {
                for (int i = 0; i < 2; i++)
                {
                    flow_textures[i, mat] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
                    flow_textures[i, mat].useMipMap = false;
                    flow_textures[i, mat].autoGenerateMips = false;
                    flow_textures[i, mat].filterMode = FilterMode.Point;
                }

                flow_materials[mat] = new Material(flow_material);
                flow_materials[mat].SetTexture("_HeightTex", height_texture);
                flow_materials[mat].SetFloat("_TexelWidth", 1.0f / width);
                flow_materials[mat].SetFloat("_TexelHeight", 1.0f / height);
                flow_materials[mat].SetFloat("_NumElements", (float)flows.size + (float)solids.size);
                flow_materials[mat].SetFloat("_FlowDivisor", 5.0f + (float)flows.size);
                flow_materials[mat].SetFloat("_FlowGradient", 0);
            }

            // Water
            {
                flow_materials[(int)flows.water].SetFloat("_FlowGradient", -1.0f / height);

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
                Graphics.Blit(initial_data, flow_textures[0, (int)flows.water]);
                Graphics.Blit(initial_data, flow_textures[1, (int)flows.water]);
            }

            // Mud
            {
                flow_materials[(int)flows.mud].SetFloat("_FlowGradient", 1.0f / height);

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
                Graphics.Blit(initial_data, flow_textures[0, (int)flows.mud]);
                Graphics.Blit(initial_data, flow_textures[1, (int)flows.mud]);
            }
        }

        // Solids
        {
            // Set up solid common attributes
            for (int mat = 0; mat < (int)solids.size; mat++)
            {
                for (int i = 0; i < 2; i++)
                {
                    solid_textures[i, mat] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
                    solid_textures[i, mat].useMipMap = false;
                    solid_textures[i, mat].autoGenerateMips = false;
                    solid_textures[i, mat].filterMode = FilterMode.Point;
                }
            }

            // Dirt
            {
                Texture2D initial_data = new Texture2D(width, height);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (y == 0 || y == 16 || y == height - 1 || x == 0 || x == 16 || x == width - 1)
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
                Graphics.Blit(initial_data, solid_textures[0, (int)solids.dirt]);
                Graphics.Blit(initial_data, solid_textures[1, (int)solids.dirt]);
            }
        }
        
        // Set up height map
        {
            for (int i = 0; i < 2; i++)
            {
                height_material_flip_flops[i] = new Material(height_material);
                height_material_flip_flops[i].SetFloat("_TexelWidth", 1.0f / width);
                height_material_flip_flops[i].SetFloat("_TexelHeight", 1.0f / height);
                height_material_flip_flops[i].SetFloat("_NumElements", (float)flows.size + (float)solids.size);
                height_material_flip_flops[i].SetTexture("_WaterTex", flow_textures[i, (int)flows.water]);
                height_material_flip_flops[i].SetTexture("_MudTex", flow_textures[i, (int)flows.mud]);
                height_material_flip_flops[i].SetTexture("_DirtTex", solid_textures[i, (int)solids.dirt]);

                Graphics.Blit(null, height_texture, height_material_flip_flops[i]);
            }
        }

        // Set up the world renderer
        {
            world_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            world_texture.useMipMap = false;
            world_texture.autoGenerateMips = false;
            world_texture.wrapMode = TextureWrapMode.Repeat;
            world_texture.filterMode = FilterMode.Point;

            mesh_renderer.material.SetTexture("_MainTex", world_texture);

            for (int i = 0; i < 2; i++)
            {
                world_material_flip_flops[i] = new Material(world_material);
                world_material_flip_flops[i].SetFloat("_TexelWidth", 1.0f / width);
                world_material_flip_flops[i].SetFloat("_TexelHeight", 1.0f / height);
                world_material_flip_flops[i].SetFloat("_NumElements", (float)flows.size);
                world_material_flip_flops[i].SetTexture("_WaterTex", flow_textures[i, (int)flows.water]);
                world_material_flip_flops[i].SetTexture("_MudTex", flow_textures[i, (int)flows.mud]);
                world_material_flip_flops[i].SetTexture("_DirtTex", solid_textures[i, (int)solids.dirt]);
            }
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
            RenderTexture.active = flow_textures[flip_flop, (int)flows.water];

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(width, height);
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
            Graphics.Blit(tex, flow_textures[flip_flop, (int)flows.water]);

            // Restore previously active render texture
            RenderTexture.active = currentActiveRT;
        }

        // On mouse click right
        if (Input.GetMouseButton(1))
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            // Set the supplied RenderTexture as the active one
            RenderTexture.active = solid_textures[flip_flop, (int)solids.dirt];

            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(width, height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.SetPixel(pos_grid.x, pos_grid.y, Color.red);
            tex.Apply();

            // TODO: Hack? This line shouldn't be required but somehow needs to be here.
            // Also the "proper" way to do this would be to write a shader to modify the texture then run the shader
            Graphics.Blit(tex, solid_textures[flip_flop, (int)solids.dirt]);

            // Restore previously active render texture
            RenderTexture.active = currentActiveRT;
        }
        
        // Make the simulation run a lot faster than the framerate
        float time_step = 1f / update_rate;
        for (float i = 0; i < Time.deltaTime; i += time_step)
        {
            // Flip the buffer
            flip_flop = 1 - flip_flop;

            // Run the shaders for flowable elements
            Graphics.Blit(flow_textures[1 - flip_flop, (int)flows.water], flow_textures[flip_flop, (int)flows.water], flow_materials[(int)flows.water]);
            Graphics.Blit(flow_textures[1 - flip_flop, (int)flows.mud], flow_textures[flip_flop, (int)flows.mud], flow_materials[(int)flows.mud]);

            // Update the solid elements
            for (int j = 0; j < (int)solids.size; j++)
            {
                Graphics.Blit(solid_textures[1 - flip_flop, j], solid_textures[flip_flop, j]); 
            }

            // Write the new height maps for use next update
            Graphics.Blit(null, height_texture, height_material_flip_flops[flip_flop]);
        }

        // Use Blit to run the world shader (which references the water and dirt textures) and save the result to render_texture
        Graphics.Blit(null, world_texture, world_material_flip_flops[flip_flop]);
    }
}
