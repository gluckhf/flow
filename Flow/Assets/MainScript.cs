using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainScript : MonoBehaviour
{
    public MeshRenderer mesh_renderer;
        
    // Material list
    private enum material
    {
        // Solids
        dirt = 0,
        copper,
        // Liquids
        water,
        lava,
        // Gasses
        steam,
        // Then do state changes
        water_to_steam,
        steam_to_water,
        // Then finalize amount of anything that could have changed
        finalize_water,
        finalize_steam,
        // Height needs to be updated after the flows
        height,
        // Heat movement depends on height and flow variables
        heat_movement,
        // Heat spread is calculated after movement
        heat_flow,
        // Then the world is drawn
        world,
        size
    }

    // Properties of each material
    private Material[] materials = new Material[(int)material.size];
    private int[] texture_source = new int[(int)material.size];
    private RenderTexture[,] textures = new RenderTexture[2, (int)material.size];
        
    public Material element_material;
    public Material height_material;
    public Material heat_material;
    public Material temperature_material;
    public Material world_material;
    public Material state_material;
    public Material finalization_material;  

    // Provides a link to the debug text
    public Text DebugText;

    // World size - powers of 2 for optimal efficiency
    public int width = 256;
    public int height = 128;
    
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
        copper,
        heat,
        size
    }

    int selected_element = 0;
    bool invert_selection = false;

    string element_selection_text = "";


    /// <summary>
    /// Initializes all textures to have correct filtering / mips / etc. properties
    /// and sets them to all black (full zeroes)
    /// </summary>
    private void InitializeTextures()
    {
        // Initialize the data to black (all zeroes)
        Texture2D initial_data = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                initial_data.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }
        initial_data.Apply();

        // Create each texture master (0) and slave (1) and blit to their initial data
        for (int tex = 0; tex < (int)material.size; tex++)
        {
            for (int i = 0; i < 2; i++)
            {
                textures[i, tex] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                textures[i, tex].useMipMap = false;
                textures[i, tex].autoGenerateMips = false;
                textures[i, tex].wrapMode = TextureWrapMode.Repeat;
                textures[i, tex].filterMode = FilterMode.Point;

                Graphics.Blit(initial_data, textures[i, tex]);
            }
        }

        // Destroy the initial data texture to prevent memory leak
        UnityEngine.Object.Destroy(initial_data);
    }

    private void InitializeMaterials()
    {
        // Define the type of the materials
        materials[(int)material.dirt] = new Material(element_material);
        texture_source[(int)material.dirt] = (int)material.dirt;
        materials[(int)material.dirt].SetFloat("_FlowDivisor", 0.0f);

        materials[(int)material.copper] = new Material(element_material);
        texture_source[(int)material.copper] = (int)material.copper;
        materials[(int)material.copper].SetFloat("_FlowDivisor", 0.0f);

        materials[(int)material.water] = new Material(element_material);
        texture_source[(int)material.water] = (int)material.water;
        materials[(int)material.water].SetFloat("_FlowDivisor", 10.0f);
        materials[(int)material.water].SetFloat("_FlowGradient", -1.0f / height);

        materials[(int)material.lava] = new Material(element_material);
        texture_source[(int)material.lava] = (int)material.lava;
        materials[(int)material.lava].SetFloat("_FlowDivisor", 30.0f);
        materials[(int)material.lava].SetFloat("_FlowGradient", -1.0f / height);

        materials[(int)material.steam] = new Material(element_material);
        texture_source[(int)material.steam] = (int)material.steam;
        materials[(int)material.steam].SetFloat("_FlowDivisor", 6.0f);
        materials[(int)material.steam].SetFloat("_FlowGradient", 1.0f / height);

        materials[(int)material.height] = new Material(height_material);
        texture_source[(int)material.height] = (int)material.height;

        materials[(int)material.heat_movement] = new Material(heat_material);
        texture_source[(int)material.heat_movement] = (int)material.heat_movement;

        materials[(int)material.heat_flow] = new Material(temperature_material);
        texture_source[(int)material.heat_flow] = (int)material.heat_movement;
        materials[(int)material.heat_flow].SetFloat("_FlowDivisor", 5.0f);

        materials[(int)material.water_to_steam] = new Material(state_material);
        texture_source[(int)material.water_to_steam] = (int)material.steam;
        materials[(int)material.water_to_steam].SetFloat("_TransitionTemperature", 0.35f);
        materials[(int)material.water_to_steam].SetFloat("_Hysteresis", 0.05f);
        materials[(int)material.water_to_steam].SetTexture("_InputTex", textures[0, (int)material.water]);
        
        materials[(int)material.steam_to_water] = new Material(state_material);
        texture_source[(int)material.steam_to_water] = (int)material.water;
        materials[(int)material.steam_to_water].SetFloat("_TransitionTemperature", -0.35f);
        materials[(int)material.steam_to_water].SetFloat("_Hysteresis", 0.05f);
        materials[(int)material.steam_to_water].SetTexture("_InputTex", textures[0, (int)material.steam]);

        materials[(int)material.finalize_water] = new Material(finalization_material);
        texture_source[(int)material.finalize_water] = (int)material.water;

        materials[(int)material.finalize_steam] = new Material(finalization_material);
        texture_source[(int)material.finalize_steam] = (int)material.steam;

        materials[(int)material.world] = new Material(world_material);
        texture_source[(int)material.world] = (int)material.world;

        // For each material, add references to the textures
        for (int mat = 0; mat < (int)material.size; mat++)
        {
            // Set common properties
            materials[mat].SetFloat("_TexelWidth", 1.0f / width);
            materials[mat].SetFloat("_TexelHeight", 1.0f / height);

            materials[mat].SetTexture("_DirtTex", textures[0, (int)material.dirt]);
            materials[mat].SetTexture("_CopperTex", textures[0, (int)material.copper]);
            materials[mat].SetTexture("_WaterTex", textures[0, (int)material.water]);
            materials[mat].SetTexture("_LavaTex", textures[0, (int)material.lava]);
            materials[mat].SetTexture("_SteamTex", textures[0, (int)material.steam]);
            materials[mat].SetTexture("_HeightTex", textures[0, (int)material.height]);
            materials[mat].SetTexture("_HeatTex", textures[0, (int)material.heat_movement]);
        }
    }

    // Use this for initialization
    void Start()
    {
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

        InitializeTextures();
        InitializeMaterials();

        // Special initialization for dirt
        Texture2D initial_data = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if ((y >= 0 && y <= 16) || y == height - 1 || x == 16 || (x >= 32 && x < 36) || x == 48 || x == 64 || x == width - 16)
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
        Graphics.Blit(initial_data, textures[0, (int)material.dirt]);
        UnityEngine.Object.Destroy(initial_data);
        
        // Set up height map
        Graphics.Blit(null, textures[0, (int)material.height], materials[(int)material.height]);
        
        // Set up the world renderer mesh
        {
            mesh_renderer.material.SetTexture("_MainTex", textures[0, (int)material.world]);
        }
    }

    // Update is called once per frame
    /// <summary>
    /// 
    /// </summary>
    void Update()
    {

        // Convert mouse position to Grid Coordinates
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 local_pos = transform.InverseTransformPoint(pos);
        Vector3 texture_pos = local_pos + Vector3.one * 0.5f;
        Vector2Int pos_grid = new Vector2Int((int)(texture_pos.x * width), (int)(texture_pos.y * height));

        // Get the string of what characters were pressed last frame
        foreach (char c in Input.inputString)
        {
            // Numbers select elements
            if ('0' <= c && c < '0' + (int)element_selection.size)
            {
                if (c == '0')
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


        string additional_text = "";

        // Additional text
        {
            RenderTexture.active = textures[0, (int)material.heat_movement];
            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            var pixel = tex.GetPixel(pos_grid.x, pos_grid.y);

            additional_text = pixel.a.ToString();
            UnityEngine.Object.Destroy(tex);
        }

        DebugText.text = pos_grid.ToString() + "\n" + additional_text + "\n" + element_selection_text;

        // On mouse clicks
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            for (int i = 1; i < (int)element_selection.size; i++)
            {
                if ((i != selected_element) == invert_selection)
                {
                    float temperature = 0.0f;

                    // Set the selected RenderTexture as the active one
                    switch ((element_selection)i)
                    {
                        case element_selection.dirt:
                            RenderTexture.active = textures[0, (int)material.dirt];
                            temperature = 0.30f;
                            break;
                        case element_selection.copper:
                            RenderTexture.active = textures[0, (int)material.copper];
                            temperature = 0.30f;
                            break;
                        case element_selection.water:
                            RenderTexture.active = textures[0, (int)material.water];
                            temperature = 0.30f;
                            break;
                        case element_selection.steam:
                            RenderTexture.active = textures[0, (int)material.steam];
                            temperature = 0.40f;
                            break;
                        case element_selection.lava:
                            RenderTexture.active = textures[0, (int)material.lava];
                            temperature = 0.90f;
                            break;
                        case element_selection.heat:
                            RenderTexture.active = textures[0, (int)material.heat_movement];
                            break;
                    }

                    // Create a new Texture2D and read the RenderTexture image into it
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                    tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                    
                    // Don't place all inverted elements
                    if (i == selected_element)
                    {
                        // Place lots
                        if (Input.GetMouseButton(0))
                        {
                            for (int x = -1; x < 1; x++)
                            {
                                for (int y = -1; y < 1; y++)
                                {
                                    var temp = tex.GetPixel(pos_grid.x + x, pos_grid.y + y);
                                    tex.SetPixel(pos_grid.x + x, pos_grid.y + y,
                                        new Color(
                                        Mathf.Min(temp.r + 0.3f, 0.5f),
                                        temp.g, temp.b, temp.a));
                                }
                            }
                        }

                        // Place one
                        if (Input.GetMouseButton(2))
                        {
                            
                            var temp = tex.GetPixel(pos_grid.x, pos_grid.y);
                            if (temp.r < 1.0f)
                            {
                                var original_amount = temp.r;
                                var additional_amount = Mathf.Min(temp.r + 0.5f, 1.0f) - original_amount;
                                tex.SetPixel(pos_grid.x, pos_grid.y,
                                    new Color(original_amount + additional_amount,
                                    temp.g, temp.b, temp.a));

                                // If we're not placing heat, place some heat
                                if ((element_selection)i != element_selection.heat)
                                {
                                    // Get the heat texture to add additional heat
                                    RenderTexture.active = textures[0, (int)material.heat_movement];
                                    Texture2D additional_heat_tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                                    additional_heat_tex.ReadPixels(new Rect(0, 0, additional_heat_tex.width, additional_heat_tex.height), 0, 0);

                                    RenderTexture.active = textures[0, (int)material.height];
                                    Texture2D height_tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                                    height_tex.ReadPixels(new Rect(0, 0, height_tex.width, height_tex.height), 0, 0);

                                    var heat_pixel = additional_heat_tex.GetPixel(pos_grid.x, pos_grid.y);
                                    var height_pixel = height_tex.GetPixel(pos_grid.x, pos_grid.y);
                                    var original_total = height_pixel.r;
                                    var final_total = original_total + additional_amount;
                                    
                                    // Add the height to the height texture
                                    height_tex.SetPixel(pos_grid.x, pos_grid.y,
                                        new Color(final_total,
                                        height_pixel.g, height_pixel.b, height_pixel.a));

                                    // Modify the heat of the heat texture to reach the appropriate temperature
                                    additional_heat_tex.SetPixel(pos_grid.x, pos_grid.y,
                                        new Color(temperature * final_total,
                                        heat_pixel.g, heat_pixel.b, temperature));

                                    additional_heat_tex.Apply();
                                    height_tex.Apply();

                                    Graphics.Blit(additional_heat_tex, textures[0, (int)material.heat_movement]);
                                    Graphics.Blit(height_tex, textures[0, (int)material.height]);

                                    UnityEngine.Object.Destroy(additional_heat_tex);
                                    UnityEngine.Object.Destroy(height_tex);
                                }
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
                                var temp = tex.GetPixel(pos_grid.x + x, pos_grid.y + y);
                                tex.SetPixel(pos_grid.x + x, pos_grid.y + y,
                                    new Color(
                                    Mathf.Max(temp.r - 0.3f, 0.0f),
                                    temp.g, temp.b, temp.a));
                            }
                        }
                    }

                    tex.Apply();
                   
                    
                    // TODO: Hack? This switch statement to Blit shouldn't be required but somehow needs to be here.
                    // Also the "proper" way to do this would be to write a shader to modify the texture then run the shader
                    switch ((element_selection)i)
                    {
                        case element_selection.dirt:
                            Graphics.Blit(tex, textures[0, (int)material.dirt]);
                            break;
                        case element_selection.copper:
                            Graphics.Blit(tex, textures[0, (int)material.copper]);
                            break;
                        case element_selection.water:
                            Graphics.Blit(tex, textures[0, (int)material.water]);
                            break;
                        case element_selection.steam:
                            Graphics.Blit(tex, textures[0, (int)material.steam]);
                            break;
                        case element_selection.lava:
                            Graphics.Blit(tex, textures[0, (int)material.lava]);
                            break;
                        case element_selection.heat:
                            Graphics.Blit(tex, textures[0, (int)material.heat_movement]);
                            break;
                    }
                    
                    // Destroy the textures to stop memory leaks
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
            // Run the shaders
            for (int mat = 0; mat < (int)material.size; mat++)
            {
                // Run the texture through the assigned material (implements a certain shader)
                Graphics.Blit(textures[0, texture_source[mat]], textures[1, texture_source[mat]], materials[mat]);

                // Keep the master texture in index 0
                Graphics.Blit(textures[1, texture_source[mat]], textures[0, texture_source[mat]]);
            }
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            materials[(int)material.world].SetInt("_Highlite", selected_element);
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            materials[(int)material.world].SetInt("_Highlite", 0);
        }
    }
}
