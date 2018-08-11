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
        steam,
        lava,
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
    public int width = 256;
    public int height = 128;

    // A-B rendering buffer selection
    int flip_flop = 0;

    // Update rate (per second) - independent of framerate
    [Range(60f, 6000f)]
    public float update_rate = 2000f;

    // Element selection
    private enum element_selection
    {
         all = 0,
         water,
         steam,
         lava,
         dirt,
         size
    }

    int selected_element = 0;
    bool invert_selection = false;

    string element_selection_text = "";

    // Use this for initialization
    void Start() {
        // Initialize the element selection text
        element_selection_text = "";
        for (int i = 1; i < (int)element_selection.size; i++)
        {
            element_selection_text += i.ToString() + ":";
            if ((i != selected_element) == invert_selection)
            {
                element_selection_text += "    ";
            }
            element_selection_text += ((element_selection)i).ToString() + "\n";
        }

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
                flow_materials[(int)flows.water].SetFloat("_FlowDivisor", 5.0f + (float)flows.size + 5.0f);
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

            // Steam
            {
                flow_materials[(int)flows.steam].SetFloat("_FlowGradient", 1.0f / height);

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
                Graphics.Blit(initial_data, flow_textures[0, (int)flows.steam]);
                Graphics.Blit(initial_data, flow_textures[1, (int)flows.steam]);
            }

            // Lava
            {
                flow_materials[(int)flows.lava].SetFloat("_FlowDivisor", 25.0f + (float)flows.size + 5.0f);
                flow_materials[(int)flows.lava].SetFloat("_FlowGradient", -1.0f / height);

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
                Graphics.Blit(initial_data, flow_textures[0, (int)flows.lava]);
                Graphics.Blit(initial_data, flow_textures[1, (int)flows.lava]);
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
                height_material_flip_flops[i].SetTexture("_SteamTex", flow_textures[i, (int)flows.steam]);
                height_material_flip_flops[i].SetTexture("_LavaTex", flow_textures[i, (int)flows.lava]);
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
                world_material_flip_flops[i].SetTexture("_SteamTex", flow_textures[i, (int)flows.steam]);
                world_material_flip_flops[i].SetTexture("_LavaTex", flow_textures[i, (int)flows.lava]);
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

        // Get the string of what characters were pressed last frame
        foreach (char c in Input.inputString)
        {
            // Numbers select elements
            if('0' <= c && c < '0' + (int)element_selection.size)
            {
                if(c == '0')
                {
                    selected_element = 0;
                    invert_selection = true;
                }
                else
                {
                    int new_selected_element = (int)(c - '0');
                    if (new_selected_element != selected_element)
                    {
                        invert_selection = false;
                    }
                    else
                    {
                        invert_selection = !invert_selection;
                    }
                    selected_element = new_selected_element;
                }

                // Update the text
                element_selection_text = "";
                for (int i = 1; i < (int)element_selection.size; i++)
                {
                    element_selection_text += i.ToString() + ":";
                    if ((i != selected_element) == invert_selection)
                    {
                        element_selection_text += "    ";
                    }
                    element_selection_text += ((element_selection)i).ToString() + "\n";
                }
            }
        }
          
        DebugText.text = pos_grid.ToString() + "\n" + element_selection_text;

        // On mouse clicks
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            for (int i = 1; i < (int)element_selection.size; i++)
            {
                if ((i != selected_element) == invert_selection)
                {
                    // Set the selected RenderTexture as the active one
                    switch ((element_selection)i)
                    {
                        case element_selection.dirt:
                            RenderTexture.active = solid_textures[flip_flop, (int)solids.dirt];
                            break;
                        case element_selection.water:
                            RenderTexture.active = flow_textures[flip_flop, (int)flows.water];
                            break;
                        case element_selection.steam:
                            RenderTexture.active = flow_textures[flip_flop, (int)flows.steam];
                            break;
                        case element_selection.lava:
                            RenderTexture.active = flow_textures[flip_flop, (int)flows.lava];
                            break;
                    }

                    // Create a new Texture2D and read the RenderTexture image into it
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                    tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

                    // Place lots
                    if (Input.GetMouseButton(0))
                    {
                        for (int x = -1; x < 1; x++)
                        {
                            for (int y = -1; y < 1; y++)
                            {
                                tex.SetPixel(pos_grid.x + x, pos_grid.y + y,
                                    new Color(
                                    Mathf.Min(tex.GetPixel(pos_grid.x + x, pos_grid.y + y).r + 0.3f, 1f),
                                    0f, 0f, 1f));
                            }
                        }
                    }

                    // Remove lots
                    if (Input.GetMouseButton(1))
                    {
                        for (int x = -1; x < 1; x++)
                        {
                            for (int y = -1; y < 1; y++)
                            {
                                tex.SetPixel(pos_grid.x + x, pos_grid.y + y,
                                    new Color(
                                    Mathf.Max(tex.GetPixel(pos_grid.x + x, pos_grid.y + y).r - 0.3f, 0f),
                                    0f, 0f, 1f));
                            }
                        }
                    }

                    // Place one
                    if (Input.GetMouseButton(2))
                    {
                        tex.SetPixel(pos_grid.x, pos_grid.y,
                                    new Color(
                                    Mathf.Min(tex.GetPixel(pos_grid.x, pos_grid.y).r + 0.5f, 1f),
                                    0f, 0f, 1f));
                    }
                    
                    tex.Apply();

                    // TODO: Hack? This switch statement to Blit shouldn't be required but somehow needs to be here.
                    // Also the "proper" way to do this would be to write a shader to modify the texture then run the shader
                    switch ((element_selection)i)
                    {
                        case element_selection.dirt:
                            Graphics.Blit(tex, solid_textures[flip_flop, (int)solids.dirt]);
                            break;
                        case element_selection.water:
                            Graphics.Blit(tex, flow_textures[flip_flop, (int)flows.water]);
                            break;
                        case element_selection.steam:
                            Graphics.Blit(tex, flow_textures[flip_flop, (int)flows.steam]);
                            break;
                        case element_selection.lava:
                            Graphics.Blit(tex, flow_textures[flip_flop, (int)flows.lava]);
                            break;
                    }

                    // Destroy the texture to stop memory leaks
                    UnityEngine.Object.Destroy(tex);
                }
            }

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
            for (int j = 0; j < (int)flows.size; j++)
            {
                Graphics.Blit(flow_textures[1 - flip_flop, j], flow_textures[flip_flop, j], flow_materials[j]);
            }

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
