﻿using System.Collections;
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
        obsidian,
        // Liquids
        water,
        lava,
        // Gasses
        steam,
        // Then do state changes
        water_to_steam,
        steam_to_water,
        dirt_to_lava,
        lava_to_dirt,
        obsidian_to_lava,
        lava_to_obsidian,
        // Then finalize amount of anything that could have changed
        finalize_water,
        finalize_steam,
        finalize_dirt,
        finalize_lava,
        finalize_obsidian,
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

    // Special material for trimming the edges of the textures
    private RenderTexture trim_texture;
    public Material trim_material;

    // Provides a link to the debug text
    public Text DebugText;

    // World size - powers of 2 for optimal efficiency
    public int width = 256;
    public int height = 128;
    private int edge_trim_pixels = 1;
    [Range(-240f, -24000f)]
    public float gravity_strength = -6000f;
    [Range(240f, 24000f)]
    public float bouyancy_strength = 600f;

    // Update rate (per second) - independent of framerate
    [Range(60f, 6000f)]
    public float update_rate = 1200f;

    // Element selection
    private enum element_selection
    {
        dirt = 1,
        copper,
        obsidian,
        water,
        lava,
        steam,
        heat,
        size
    }
    element_selection selected_element = element_selection.dirt;
    string element_selection_text = "";


    /// <summary>
    /// Initializes all textures to have correct filtering / mips / etc. properties
    /// and sets them to all black (full zeroes)
    /// </summary>
    private void InitializeTextures()
    {
        // Initialize the data to black (all zeroes)
        Texture2D blank_texture = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                blank_texture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

        blank_texture.Apply();

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

                Graphics.Blit(blank_texture, textures[i, tex]);
            }
        }

        UnityEngine.Object.Destroy(blank_texture);

        // Special initialization for trim texture
        trim_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        trim_texture.useMipMap = false;
        trim_texture.autoGenerateMips = false;
        trim_texture.wrapMode = TextureWrapMode.Repeat;
        trim_texture.filterMode = FilterMode.Point;

        Texture2D initial_data = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y < edge_trim_pixels || y >= height - edge_trim_pixels || x < edge_trim_pixels || x >= width - edge_trim_pixels)
                {
                    initial_data.SetPixel(x, y, new Color(1, 0, 0, 0));
                }
                else
                {
                    initial_data.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }
        initial_data.Apply();
        Graphics.Blit(initial_data, trim_texture);
        UnityEngine.Object.Destroy(initial_data);
    }

    private void InitializeMaterials()
    {
        // Define the type of the materials
        //Note: Anything less than 4 regarding the flow divisors will lead to instability
        materials[(int)material.dirt] = new Material(element_material);
        texture_source[(int)material.dirt] = (int)material.dirt;
        materials[(int)material.dirt].SetFloat("_FlowDivisorNS", 0.0f);
        materials[(int)material.dirt].SetFloat("_FlowDivisorEW", 0.0f);

        materials[(int)material.copper] = new Material(element_material);
        texture_source[(int)material.copper] = (int)material.copper;
        materials[(int)material.copper].SetFloat("_FlowDivisorNS", 0.0f);
        materials[(int)material.copper].SetFloat("_FlowDivisorEW", 0.0f);

        materials[(int)material.obsidian] = new Material(element_material);
        texture_source[(int)material.obsidian] = (int)material.obsidian;
        materials[(int)material.obsidian].SetFloat("_FlowDivisorNS", 6.0f);
        materials[(int)material.obsidian].SetFloat("_FlowDivisorEW", 1000.0f);
        materials[(int)material.obsidian].SetFloat("_FlowGradient", gravity_strength / height / update_rate);

        materials[(int)material.water] = new Material(element_material);
        texture_source[(int)material.water] = (int)material.water;
        materials[(int)material.water].SetFloat("_FlowDivisorNS", 6.0f);
        materials[(int)material.water].SetFloat("_FlowDivisorEW", 6.0f);
        materials[(int)material.water].SetFloat("_FlowGradient", gravity_strength / height / update_rate);

        materials[(int)material.lava] = new Material(element_material);
        texture_source[(int)material.lava] = (int)material.lava;
        materials[(int)material.lava].SetFloat("_FlowDivisorNS", 6.0f);
        materials[(int)material.lava].SetFloat("_FlowDivisorEW", 30.0f);
        materials[(int)material.lava].SetFloat("_FlowGradient", gravity_strength / height / update_rate);

        materials[(int)material.steam] = new Material(element_material);
        texture_source[(int)material.steam] = (int)material.steam;
        materials[(int)material.steam].SetFloat("_FlowDivisorNS", 6.0f);
        materials[(int)material.steam].SetFloat("_FlowDivisorEW", 12.0f);
        materials[(int)material.steam].SetFloat("_FlowGradient", bouyancy_strength / height / update_rate);

        materials[(int)material.height] = new Material(height_material);
        texture_source[(int)material.height] = (int)material.height;

        materials[(int)material.heat_movement] = new Material(heat_material);
        texture_source[(int)material.heat_movement] = (int)material.heat_movement;

        materials[(int)material.heat_flow] = new Material(temperature_material);
        texture_source[(int)material.heat_flow] = (int)material.heat_movement;
        materials[(int)material.heat_flow].SetFloat("_FlowDivisorNS", 4.0f);
        materials[(int)material.heat_flow].SetFloat("_FlowDivisorEW", 4.0f);

        materials[(int)material.dirt_to_lava] = new Material(state_material);
        texture_source[(int)material.dirt_to_lava] = (int)material.lava;
        materials[(int)material.dirt_to_lava].SetFloat("_TransitionHotTemperature", 1800.0f); // Transition to lava when hot
        materials[(int)material.dirt_to_lava].SetFloat("_TransitionColdTemperature", 0.00f); // Does not transition to dirt
        materials[(int)material.dirt_to_lava].SetTexture("_InputTex", textures[0, (int)material.dirt]);

        materials[(int)material.lava_to_dirt] = new Material(state_material);
        texture_source[(int)material.lava_to_dirt] = (int)material.dirt;
        materials[(int)material.lava_to_dirt].SetFloat("_TransitionHotTemperature", -1800.0f); // Transition to lava when hot
        materials[(int)material.lava_to_dirt].SetFloat("_TransitionColdTemperature", -0.00f); // Does not transition to dirt
        materials[(int)material.lava_to_dirt].SetTexture("_InputTex", textures[0, (int)material.lava]);

        materials[(int)material.obsidian_to_lava] = new Material(state_material);
        texture_source[(int)material.obsidian_to_lava] = (int)material.lava;
        materials[(int)material.obsidian_to_lava].SetFloat("_TransitionHotTemperature", 1800.0f);
        materials[(int)material.obsidian_to_lava].SetFloat("_TransitionColdTemperature", 1300.0f);
        materials[(int)material.obsidian_to_lava].SetTexture("_InputTex", textures[0, (int)material.obsidian]);

        materials[(int)material.lava_to_obsidian] = new Material(state_material);
        texture_source[(int)material.lava_to_obsidian] = (int)material.obsidian;
        materials[(int)material.lava_to_obsidian].SetFloat("_TransitionHotTemperature", -1800.0f);
        materials[(int)material.lava_to_obsidian].SetFloat("_TransitionColdTemperature", -1300.0f);
        materials[(int)material.lava_to_obsidian].SetTexture("_InputTex", textures[0, (int)material.lava]);

        materials[(int)material.water_to_steam] = new Material(state_material);
        texture_source[(int)material.water_to_steam] = (int)material.steam;
        materials[(int)material.water_to_steam].SetFloat("_TransitionHotTemperature", 101.0f);
        materials[(int)material.water_to_steam].SetFloat("_TransitionColdTemperature", 99.0f);
        materials[(int)material.water_to_steam].SetTexture("_InputTex", textures[0, (int)material.water]);

        materials[(int)material.steam_to_water] = new Material(state_material);
        texture_source[(int)material.steam_to_water] = (int)material.water;
        materials[(int)material.steam_to_water].SetFloat("_TransitionHotTemperature", -101.0f);
        materials[(int)material.steam_to_water].SetFloat("_TransitionColdTemperature", -99.0f);
        materials[(int)material.steam_to_water].SetTexture("_InputTex", textures[0, (int)material.steam]);

        materials[(int)material.finalize_water] = new Material(finalization_material);
        texture_source[(int)material.finalize_water] = (int)material.water;

        materials[(int)material.finalize_steam] = new Material(finalization_material);
        texture_source[(int)material.finalize_steam] = (int)material.steam;

        materials[(int)material.finalize_dirt] = new Material(finalization_material);
        texture_source[(int)material.finalize_dirt] = (int)material.dirt;

        materials[(int)material.finalize_lava] = new Material(finalization_material);
        texture_source[(int)material.finalize_lava] = (int)material.lava;

        materials[(int)material.finalize_obsidian] = new Material(finalization_material);
        texture_source[(int)material.finalize_obsidian] = (int)material.obsidian;

        materials[(int)material.world] = new Material(world_material);
        texture_source[(int)material.world] = (int)material.world;

        // For each material, add references to the textures
        for (int mat = 0; mat < (int)material.size; mat++)
        {
            // Set common properties
            materials[mat].SetFloat("_TexelWidth", 1.0f / width);
            materials[mat].SetFloat("_TexelHeight", 1.0f / height);
            materials[mat].SetFloat("_ElementCapacity", GetCapacity((material)mat));

            materials[mat].SetTexture("_DirtTex", textures[0, (int)material.dirt]);
            materials[mat].SetTexture("_CopperTex", textures[0, (int)material.copper]);
            materials[mat].SetTexture("_ObsidianTex", textures[0, (int)material.obsidian]);
            materials[mat].SetTexture("_WaterTex", textures[0, (int)material.water]);
            materials[mat].SetTexture("_LavaTex", textures[0, (int)material.lava]);
            materials[mat].SetTexture("_SteamTex", textures[0, (int)material.steam]);
            materials[mat].SetTexture("_HeightTex", textures[0, (int)material.height]);
            materials[mat].SetTexture("_HeatTex", textures[0, (int)material.heat_movement]);
        }

        // Special initialization for trim material
        trim_material.SetTexture("_TrimTex", trim_texture);
    }

    private float GetCapacity(material mat)
    {
        float cap_scaling = 4.1813f;

        // Heat capacities determine how much heat the element stores per degree of temperature
        // Must be the same values as in TemperatureShader or funny things will happen
        // https://en.wikipedia.org/wiki/Table_of_specific_heat_capacities

        switch (mat)
        {
            case material.water:
                return 4.1813f / cap_scaling;
            case material.steam:
                return 2.0800f / cap_scaling;
            case material.lava:
                return 1.5600f / cap_scaling;
            case material.obsidian:
                return 1.0000f / cap_scaling;
            case material.dirt:
                return 0.8000f / cap_scaling;
            case material.copper:
                return 0.3850f / cap_scaling;
        }

        return 0.0f;
    }

    private void UpdateElementSelectionText()
    {
        element_selection_text = "";
        for (int i = 1; i < (int)element_selection.size; i++)
        {
            element_selection_text += i.ToString() + ":";
            if ((element_selection)i == selected_element)
            {
                element_selection_text += "    ";
            }
            element_selection_text += ((element_selection)i).ToString() + "\n";
        }
    }

    // Use this for initialization
    void Start()
    {
        // Initialize the element selection text
        UpdateElementSelectionText();

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
                    initial_data.SetPixel(x, y, new Color(1, 0, 0, 0));
                }
                else
                {
                    initial_data.SetPixel(x, y, new Color(0, 0, 0, 0));
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

    private void PlaceElement(Vector2Int pos_grid, element_selection sel, bool change_heat, float amount, int radius)
    {
        float small = 0.000001f;

        // TODO: The "proper" way to do this would be to write a shader to modify the texture then run the shader

        // Remember currently active render texture
        RenderTexture currentActiveRT = RenderTexture.active;

        float temperature = 0.0f;
        float capacity = 0.0f;
        // Set the selected RenderTexture as the active one
        switch (sel)
        {
            case element_selection.dirt:
                RenderTexture.active = textures[0, (int)material.dirt];
                temperature = 24f;
                capacity = GetCapacity(material.dirt);
                break;
            case element_selection.copper:
                RenderTexture.active = textures[0, (int)material.copper];
                temperature = 24f;
                capacity = GetCapacity(material.copper);
                break;
            case element_selection.obsidian:
                RenderTexture.active = textures[0, (int)material.obsidian];
                temperature = 18f;
                capacity = GetCapacity(material.obsidian);
                break;
            case element_selection.water:
                RenderTexture.active = textures[0, (int)material.water];
                temperature = 24f;
                capacity = GetCapacity(material.water);
                break;
            case element_selection.lava:
                RenderTexture.active = textures[0, (int)material.lava];
                temperature = 1800f;
                capacity = GetCapacity(material.lava);
                break;
            case element_selection.steam:
                RenderTexture.active = textures[0, (int)material.steam];
                temperature = 100f;
                capacity = GetCapacity(material.steam);
                break;
            case element_selection.heat:
                RenderTexture.active = textures[0, (int)material.heat_movement];
                break;
            default:
                return;
        }

        // Create a new Texture2D and read the RenderTexture image into it
        Texture2D element_tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        element_tex.ReadPixels(new Rect(0, 0, element_tex.width, element_tex.height), 0, 0);

        // Get the height texture
        RenderTexture.active = textures[0, (int)material.height];
        Texture2D height_tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        height_tex.ReadPixels(new Rect(0, 0, height_tex.width, height_tex.height), 0, 0);

        // Get the heat texture
        RenderTexture.active = textures[0, (int)material.heat_movement];
        Texture2D additional_heat_tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        additional_heat_tex.ReadPixels(new Rect(0, 0, additional_heat_tex.width, additional_heat_tex.height), 0, 0);

        // Change every pixel in radius
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (Mathf.Abs(x) * Mathf.Abs(x) + Mathf.Abs(y) * Mathf.Abs(y) < radius * radius)
                {
                    int pix_x = pos_grid.x + x;
                    int pix_y = pos_grid.y + y;
                    var element_pixel = element_tex.GetPixel(pix_x, pix_y);
                    var height_pixel = height_tex.GetPixel(pix_x, pix_y);

                    // Calculate change in amount
                    float delta_amount = 0.0f;
                    if (element_pixel.r > 0.0f && amount < 0.0f)
                    {
                        delta_amount = Mathf.Max(element_pixel.r + amount, 0.0f) - element_pixel.r;
                    }
                    else if (element_pixel.r < 1.0f && amount > 0.0f)
                    {
                        delta_amount = Mathf.Min(element_pixel.r + amount, 1.0f) - element_pixel.r;
                    }
                    else
                    {
                        continue;
                    }

                    // If there is amount to change, change it
                    element_tex.SetPixel(pix_x, pix_y,
                                    new Color(element_pixel.r + delta_amount, element_pixel.g, element_pixel.b, element_pixel.a));

                    // Change height
                    if (sel != element_selection.heat)
                    {
                        var final_height = Mathf.Max(height_pixel.r + delta_amount, 0.0f);
                        height_tex.SetPixel(pix_x, pix_y,
                            new Color(final_height, height_pixel.g, height_pixel.b, height_pixel.a));

                        // Change heat
                        if (change_heat)
                        {
                            // Get the heat texture to add additional heat
                            var heat_pixel = additional_heat_tex.GetPixel(pix_x, pix_y);
                            var new_heat_value = Mathf.Max(temperature * capacity * delta_amount + heat_pixel.r, 0.0f); ;
                            additional_heat_tex.SetPixel(pix_x, pix_y,
                                new Color(new_heat_value, heat_pixel.g, heat_pixel.b, new_heat_value / Mathf.Max(final_height, small)));
                        }
                    }
                }
            }
        }

        height_tex.Apply();
        additional_heat_tex.Apply();
        element_tex.Apply();

        Graphics.Blit(height_tex, textures[0, (int)material.height]);
        Graphics.Blit(additional_heat_tex, textures[0, (int)material.heat_movement]);

        switch (sel)
        {
            case element_selection.dirt:
                Graphics.Blit(element_tex, textures[0, (int)material.dirt]);
                break;
            case element_selection.copper:
                Graphics.Blit(element_tex, textures[0, (int)material.copper]);
                break;
            case element_selection.obsidian:
                Graphics.Blit(element_tex, textures[0, (int)material.obsidian]);
                break;
            case element_selection.water:
                Graphics.Blit(element_tex, textures[0, (int)material.water]);
                break;
            case element_selection.lava:
                Graphics.Blit(element_tex, textures[0, (int)material.lava]);
                break;
            case element_selection.steam:
                Graphics.Blit(element_tex, textures[0, (int)material.steam]);
                break;
            case element_selection.heat:
                Graphics.Blit(element_tex, textures[0, (int)material.heat_movement]);
                break;
        }

        // Destroy the textures to stop memory leaks
        UnityEngine.Object.Destroy(element_tex);
        UnityEngine.Object.Destroy(height_tex);
        UnityEngine.Object.Destroy(additional_heat_tex);

        // Restore previously active render texture
        RenderTexture.active = currentActiveRT;
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
            if ('1' <= c && c < '1' + (int)element_selection.size)
            {
                selected_element = (element_selection)(c - '0');
                UpdateElementSelectionText();
            }
        }

        string additional_text = "";

        // Additional text
        {
            RenderTexture.active = textures[0, (int)material.heat_movement];
            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex_heat = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            tex_heat.ReadPixels(new Rect(0, 0, tex_heat.width, tex_heat.height), 0, 0);

            var temperature = tex_heat.GetPixel(pos_grid.x, pos_grid.y);

            RenderTexture.active = textures[0, (int)material.height];
            // Create a new Texture2D and read the RenderTexture image into it
            Texture2D tex_height = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            tex_height.ReadPixels(new Rect(0, 0, tex_height.width, tex_height.height), 0, 0);

            var mass = tex_height.GetPixel(pos_grid.x, pos_grid.y);

            additional_text = temperature.a.ToString() + "°C\n" + mass.r.ToString() + "kg";
            UnityEngine.Object.Destroy(tex_height);
            UnityEngine.Object.Destroy(tex_heat);
        }

        DebugText.text = pos_grid.ToString() + "\n" + additional_text + "\n\n" + element_selection_text;

        // On mouse clicks
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            // Place lots
            if (Input.GetMouseButton(0))
            {
                PlaceElement(pos_grid, selected_element, true, 20.0f * Time.deltaTime, 3);
            }

            // Place some
            if (Input.GetMouseButton(2))
            {
                PlaceElement(pos_grid, selected_element, true, 100.0f * Time.deltaTime, 1);
            }

            // Remove lots
            if (Input.GetMouseButton(1))
            {
                PlaceElement(pos_grid, selected_element, true, -100.0f * Time.deltaTime, 5);
            }
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

                // Keep the master texture in index 0 and trim the edges of the map
                Graphics.Blit(textures[1, texture_source[mat]], textures[0, texture_source[mat]], trim_material);
            }
        }

        // Highliting
        if (Input.GetKeyDown(KeyCode.Space))
        {
            materials[(int)material.world].SetInt("_Highlite", (int)selected_element);
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            materials[(int)material.world].SetInt("_Highlite", 0);
        }

        // Chaos
        if (Input.GetKeyDown(KeyCode.C))
        {
            Chaos();
        }
    }

    private void Chaos()
    {
        for (int tex = (int)element_selection.dirt; tex < (int)element_selection.heat; tex++)
        {
            PlaceElement(new Vector2Int((int)(Random.value * width), (int)(Random.value * height)), (element_selection)tex, true, Random.Range(0.1f, 1.0f), (int)(Random.Range(5.0f, 55.0f)));
        }
    }
}
