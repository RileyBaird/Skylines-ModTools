﻿using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;

namespace ModTools
{
    public class DebugRenderer : MonoBehaviour
    {

        public bool drawDebugInfo = false;

        private GUIStyle normalRectStyle;
        private GUIStyle hoveredRectStyle;
        private GUIStyle infoWindowStyle;

        private UIComponent hoveredComponent;
        private readonly List<UIComponent> hoveredComponents = new List<UIComponent>();
        private long previousHash = 0;

        void Update()
        {
            var hoveredLocal = hoveredComponent;
            if (drawDebugInfo && hoveredLocal != null)
            {
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
                {
                    var uiView = FindObjectOfType<UIView>();
                    if (uiView == null)
                    {
                        return;
                    }

                    var current = hoveredLocal;
                    var refChain = new ReferenceChain();
                    refChain = refChain.Add(current);
                    while (current != null)
                    {
                        refChain = refChain.Add(current.gameObject);
                        current = current.parent;
                    }
                    ;
                    refChain = refChain.Add(uiView.gameObject);

                    var sceneExplorer = FindObjectOfType<SceneExplorer>();
                    sceneExplorer.ExpandFromRefChain(refChain.Reverse);
                    sceneExplorer.visible = true;
                }
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
                {
                    if (hoveredComponents.Count > 1 && hoveredComponent != null)
                    {
                        var index = hoveredComponents.IndexOf(hoveredComponent);
                        var newIndex = (index + hoveredComponents.Count + 1) % hoveredComponents.Count;
                        hoveredComponent = hoveredComponents[newIndex];
                    }
                }
            }
        }

        void OnGUI()
        {
            if (!drawDebugInfo)
            {
                return;
            }

            if (normalRectStyle == null)
            {
                normalRectStyle = new GUIStyle(GUI.skin.box);
                var bgTexture = new Texture2D(1, 1);
                bgTexture.SetPixel(0, 0, new Color(1.0f, 0.0f, 1.0f, 0.1f));
                bgTexture.Apply();
                normalRectStyle.normal.background = bgTexture;
                normalRectStyle.hover.background = bgTexture;
                normalRectStyle.active.background = bgTexture;
                normalRectStyle.focused.background = bgTexture;

                hoveredRectStyle = new GUIStyle(GUI.skin.box);
                bgTexture = new Texture2D(1, 1);
                bgTexture.SetPixel(0, 0, new Color(0.0f, 1.0f, 0.0f, 0.3f));
                bgTexture.Apply();
                hoveredRectStyle.normal.background = bgTexture;
                hoveredRectStyle.hover.background = bgTexture;
                hoveredRectStyle.active.background = bgTexture;
                hoveredRectStyle.focused.background = bgTexture;

                infoWindowStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = null },
                    hover = { background = null },
                    active = { background = null },
                    focused = { background = null }
                };
            }

            var uiView = FindObjectOfType<UIView>();

            if (uiView == null)
            {
                return;
            }

            UIComponent[] components = GetComponentsInChildren<UIComponent>();
            Array.Sort(components, RenderSortFunc);

            var mouse = Input.mousePosition;
            mouse.y = Screen.height - mouse.y;

            hoveredComponents.Clear();
            long hash = 0;
            for (int i = components.Length - 1; i > 0; i--)
            {
                var component = components[i];

                if (!component.isVisible)
                {
                    continue;
                }

                if (component.name == "FullScreenContainer")
                {
                    continue;
                }

                if (component.name == "PauseOutline")
                {
                    continue;
                }

                var position = component.absolutePosition;
                var size = component.size;
                var rect = CalculateRealComponentRect(position, size);
                if (rect.Contains(mouse))
                {
                    hash += CalculateHash(component);
                    hoveredComponents.Add(component);
                }
            }
            if (hoveredComponent != null && hash != previousHash)
            {
                hoveredComponent = null;
                previousHash = hash;
            }
            if (hoveredComponent == null && hoveredComponents.Count > 0)
            {
                hoveredComponent = hoveredComponents.First();
            }


            foreach (var component in components)
            {
                if (!component.isVisible)
                {
                    continue;
                }

                var position = component.absolutePosition;
                var size = component.size;
                var rect = CalculateRealComponentRect(position, size);

                GUI.Box(rect, "", hoveredComponent == component ? hoveredRectStyle : normalRectStyle);
            }

            if (hoveredComponent != null)
            {
                var coords = mouse;

                var size = new Vector2(300.0f, 300.0f);

                if (coords.x + size.x >= Screen.width)
                {
                    coords.x = Screen.width - size.x;
                }

                if (coords.y + size.y >= Screen.height)
                {
                    coords.y = Screen.height - size.y;
                }

                GUI.Window(81871, new Rect(coords.x, coords.y, size.x, size.y), DoInfoWindow, "", infoWindowStyle);
            }
        }

        Rect CalculateRealComponentRect(Vector3 absolutePosition, Vector2 size)
        {
            var dx = Screen.width / 1920.0f;
            var dy = Screen.height / 1080.0f;
            absolutePosition.x *= dx;
            absolutePosition.y *= dy;
            size.x *= dx;
            size.y *= dy;
            return new Rect(absolutePosition.x, absolutePosition.y, size.x, size.y);
        }

        void DoInfoWindow(int i)
        {
            if (hoveredComponent == null)
            {
                return;
            }
            GUI.color = Color.red;
            GUILayout.Label("[Press Ctrl+F to open it in SceneExplorer]");
            GUI.color = Color.blue;
            GUILayout.Label("[Press Ctrl+G to iterate]");
            GUI.color = Color.white;
            GUILayout.Label($"name: {hoveredComponent.name}");
            GUILayout.Label($"type: {hoveredComponent.GetType().Name}");

            if (hoveredComponent.parent != null)
            {
                GUILayout.Label($"parent: {hoveredComponent.parent?.name}");
            }

            GUILayout.Label($"anchor: {hoveredComponent.anchor}");
            GUILayout.Label($"size: {hoveredComponent.size}");
            GUILayout.Label($"position: {hoveredComponent.position}");
            GUILayout.Label($"relativePosition: {hoveredComponent.relativePosition}");
            var interactiveComponent = hoveredComponent as UIInteractiveComponent;
            if (interactiveComponent != null)
            {
                GUILayout.Label($"atlas.name: {interactiveComponent.atlas.name}");
            }
            var sprite = hoveredComponent as UISprite;
            if (sprite != null)
            {
                GUILayout.Label($"atlas.name: {sprite.atlas?.name}");
                GUILayout.Label($"spriteName: {sprite.spriteName}");
            }
            var textureSprite = hoveredComponent as UITextureSprite;
            if (textureSprite != null)
            {
                GUILayout.Label($"texture.name: {textureSprite.texture?.name}");
            }
            GUILayout.Label($"zOrder: {hoveredComponent.zOrder}");
            var hash = CalculateHash(hoveredComponent);
            GUILayout.Label($"hash: {HashUtil.HashToString(hash)}");
        }

        private long CalculateHash(UIComponent c)
        {
            return HashUtil.HashRect(new Rect(c.relativePosition.x, c.relativePosition.y,
                c.size.x, c.size.y));
        }

        private int RenderSortFunc(UIComponent lhs, UIComponent rhs)
        {
            return lhs.renderOrder.CompareTo(rhs.renderOrder);
        }

    }
}