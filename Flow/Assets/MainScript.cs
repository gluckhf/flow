﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainScript : MonoBehaviour
{

    public MeshRenderer mesh_renderer;

    // Height map
    public Material height_material;
    RenderTexture height_texture;

    // Heat map
    public Material heat_material;
    RenderTexture[] heat_textures = new RenderTexture[2];

    // Heat flow
    public Material heat2_material;

    // World compositing
    public Material world_material;
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
        copper,
        size
    }

    RenderTexture[] solid_textures = new RenderTexture[(int)solids.size];

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

        // Height texture
        {
            height_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            height_texture.useMipMap = false;
            height_texture.autoGenerateMips = false;
            height_texture.wrapMode = TextureWrapMode.Repeat;
            height_texture.filterMode = FilterMode.Point;
        }

        // Heat textures
        {
            for (int i = 0; i < 2; i++)
            {
                heat_textures[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                heat_textures[i].useMipMap = false;
                heat_textures[i].autoGenerateMips = false;
                heat_textures[i].wrapMode = TextureWrapMode.Repeat;
                heat_textures[i].filterMode = FilterMode.Point;
            }

            Texture2D initial_data = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    initial_data.SetPixel(x, y, new Color(0, 0, 0));
                }
            }
            initial_data.Apply();
            Graphics.Blit(initial_data, heat_textures[0]);
            Graphics.Blit(initial_data, heat_textures[1]);
        }

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
                    flow_textures[i, mat].wrapMode = TextureWrapMode.Repeat;
                    flow_textures[i, mat].filterMode = FilterMode.Point;
                }

                flow_materials[mat] = new Material(flow_material);
                flow_materials[mat].SetTexture("_HeightTex", height_texture);
                flow_materials[mat].SetTexture("_HeatTex", heat_textures[0]);
                flow_materials[mat].SetFloat("_TexelWidth", 1.0f / width);
                flow_materials[mat].SetFloat("_TexelHeight", 1.0f / height);
                flow_materials[mat].SetFloat("_FlowDivisor", 5.0f);
                flow_materials[mat].SetFloat("_FlowGradient", 0);
            }

            // Water
            {
                flow_materials[(int)flows.water].SetFloat("_FlowDivisor", 10.0f);
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
                flow_materials[(int)flows.steam].SetFloat("_FlowDivisor", 4.5f);
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
                flow_materials[(int)flows.lava].SetFloat("_FlowDivisor", 30.0f);
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
                solid_textures[mat] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
                solid_textures[mat].useMipMap = false;
                solid_textures[mat].autoGenerateMips = false;
                solid_textures[mat].filterMode = FilterMode.Point;
            }

            // Dirt
            {
                Texture2D initial_data = new Texture2D(width, height);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if ((y >= 16 && y <= 32) || x == 16 || (x >= 32 && x < 36) || x == 48 || x == 64)
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
                Graphics.Blit(initial_data, solid_textures[(int)solids.dirt]);
            }

            // Copper
            {
                Texture2D initial_data = new Texture2D(width, height);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        initial_data.SetPixel(x, y, new Color(0, 0, 0));
                    }
                }
                initial_data.Apply();
                Graphics.Blit(initial_data, solid_textures[(int)solids.copper]);
            }
        }

        // Heat material (flow with elements)
        {
            heat_material = new Material(heat_material);
            heat_material.SetFloat("_TexelWidth", 1.0f / width);
            heat_material.SetFloat("_TexelHeight", 1.0f / height);
            heat_material.SetTexture("_WaterTex", flow_textures[0, (int)flows.water]);
            heat_material.SetTexture("_SteamTex", flow_textures[0, (int)flows.steam]);
            heat_material.SetTexture("_LavaTex", flow_textures[0, (int)flows.lava]);
        }

        // Heat2 material (flow between elements)
        {
            heat2_material = new Material(heat2_material);
            heat2_material.SetFloat("_TexelWidth", 1.0f / width);
            heat2_material.SetFloat("_TexelHeight", 1.0f / height);
            heat2_material.SetFloat("_FlowDivisor", 5.0f);
            heat2_material.SetTexture("_HeatTex", heat_textures[0]);
            heat2_material.SetTexture("_WaterTex", flow_textures[0, (int)flows.water]);
            heat2_material.SetTexture("_SteamTex", flow_textures[0, (int)flows.steam]);
            heat2_material.SetTexture("_LavaTex", flow_textures[0, (int)flows.lava]);
            heat2_material.SetTexture("_DirtTex", solid_textures[(int)solids.dirt]);
            heat2_material.SetTexture("_CopperTex", solid_textures[(int)solids.copper]);
            heat2_material.SetTexture("_HeightTex", height_texture);
        }

        // Set up height map
        {
            height_material = new Material(height_material);
            height_material.SetFloat("_TexelWidth", 1.0f / width);
            height_material.SetFloat("_TexelHeight", 1.0f / height);
            height_material.SetTexture("_WaterTex", flow_textures[0, (int)flows.water]);
            height_material.SetTexture("_SteamTex", flow_textures[0, (int)flows.steam]);
            height_material.SetTexture("_LavaTex", flow_textures[0, (int)flows.lava]);
            height_material.SetTexture("_DirtTex", solid_textures[(int)solids.dirt]);
            height_material.SetTexture("_CopperTex", solid_textures[(int)solids.copper]);

            Graphics.Blit(null, height_texture, height_material);
        }

        // Set up the world renderer
        {
            world_texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            world_texture.useMipMap = false;
            world_texture.autoGenerateMips = false;
            world_texture.wrapMode = TextureWrapMode.Repeat;
            world_texture.filterMode = FilterMode.Point;

            mesh_renderer.material.SetTexture("_MainTex", world_texture);
            
            world_material = new Material(world_material);
            world_material.SetFloat("_TexelWidth", 1.0f / width);
            world_material.SetFloat("_TexelHeight", 1.0f / height);
            world_material.SetTexture("_WaterTex", flow_textures[0, (int)flows.water]);
            world_material.SetTexture("_SteamTex", flow_textures[0, (int)flows.steam]);
            world_material.SetTexture("_LavaTex", flow_textures[0, (int)flows.lava]);
            world_material.SetTexture("_DirtTex", solid_textures[(int)solids.dirt]);
            world_material.SetTexture("_CopperTex", solid_textures[(int)solids.copper]);
            world_material.SetTexture("_HeatTex", heat_textures[0]);
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
                            RenderTexture.active = solid_textures[(int)solids.dirt];
                            break;
                        case element_selection.copper:
                            RenderTexture.active = solid_textures[(int)solids.copper];
                            break;
                        case element_selection.water:
                            RenderTexture.active = flow_textures[0, (int)flows.water];
                            break;
                        case element_selection.steam:
                            RenderTexture.active = flow_textures[0, (int)flows.steam];
                            break;
                        case element_selection.lava:
                            RenderTexture.active = flow_textures[0, (int)flows.lava];
                            break;
                        case element_selection.heat:
                            RenderTexture.active = heat_textures[0];
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
                            tex.SetPixel(pos_grid.x, pos_grid.y,
                                new Color(
                                Mathf.Min(temp.r + 0.5f, 1.0f),
                                temp.g, temp.b, temp.a));
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
                            Graphics.Blit(tex, solid_textures[(int)solids.dirt]);
                            break;
                        case element_selection.copper:
                            Graphics.Blit(tex, solid_textures[(int)solids.copper]);
                            break;
                        case element_selection.water:
                            Graphics.Blit(tex, flow_textures[0, (int)flows.water]);
                            break;
                        case element_selection.steam:
                            Graphics.Blit(tex, flow_textures[0, (int)flows.steam]);
                            break;
                        case element_selection.lava:
                            Graphics.Blit(tex, flow_textures[0, (int)flows.lava]);
                            break;
                        case element_selection.heat:
                            Graphics.Blit(tex, heat_textures[0]);
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
            // Run the shaders for flowable elements
            for (int flowable_element = 0; flowable_element < (int)flows.size; flowable_element++)
            {
                Graphics.Blit(flow_textures[0, flowable_element],
                    flow_textures[1, flowable_element],
                    flow_materials[flowable_element]);

                // Keep the master texture in index 0
                Graphics.Blit(flow_textures[1, flowable_element],
                    flow_textures[0, flowable_element]);
            }

            // Update the solid elements
            // TODO: solid element logic? Do they collapse or anything?

            // Update the height map
            Graphics.Blit(null,
                height_texture,
                height_material);

            // Update the heat map based on what flowed in the flowables section
            Graphics.Blit(heat_textures[0],
                heat_textures[1],
                heat_material);

            // Keep the master texture in index 0
            Graphics.Blit(heat_textures[1],
                heat_textures[0]);
            
            // Run the heat flow shader to distribute heat based on temperature differences
            Graphics.Blit(null,
                heat_textures[1],
                heat2_material);

            // Keep the master texture in index 0
            Graphics.Blit(heat_textures[1],
                heat_textures[0]);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            world_material.SetInt("_Highlite", selected_element);
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            world_material.SetInt("_Highlite", 0);
        }
        // Use Blit to run the world shader (which references the water and dirt textures) and save the result to render_texture
        Graphics.Blit(null, world_texture, world_material);
    }
}
