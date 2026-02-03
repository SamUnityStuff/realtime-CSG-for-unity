using System;
using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSG.Components;
using UnityEngine.Pool;

namespace RealtimeCSGExtensions
{
    public static class RCSGExtensionUtility
    {
        internal static UnityEngine.Color WithAlpha(this UnityEngine.Color c, float alpha) { return new UnityEngine.Color(c.r, c.g, c.b, alpha); }

        // TODO: Fill out reusable list instead?
        internal static SelectedBrushSurface[] GetSelectedSurfacesAlloc()
        {
            SelectedBrushSurface[] surfaces = null;
            {
                var editModeSurface = EditModeManager.ActiveTool as EditModeSurface;
                if (editModeSurface != null)
                {
                    surfaces = editModeSurface.GetSelectedSurfaces();
                }
                
                var editModePlace = EditModeManager.ActiveTool as EditModePlace;
                if (editModePlace != null)
                {
                    // Early out: No selection!
                    if (!editModePlace.HaveBrushSelection) { return null; }
                    
                    ReadOnlySpan<CSGBrush> selectedBrushes = editModePlace.GetSelectedBrushes();
                    
                    // 1. Count surfaces
                    int surfaceCount = 0;
                    for (int i = 0; i < selectedBrushes.Length; i++)
                    {
                        var brush = selectedBrushes[i];
                        surfaceCount += brush.Shape.Surfaces.Length;
                    }
                    
                    // 2. Allocate surface pointers (TODO: make this not. a class?)
                    surfaces = new SelectedBrushSurface[surfaceCount];
                    
                    // 3. Copy surfaces
                    surfaceCount = 0;
                    for (int idxSelectedBrush = 0; idxSelectedBrush < selectedBrushes.Length; idxSelectedBrush++)
                    {
                        var brush = selectedBrushes[idxSelectedBrush];
                        var brushSurfaces = brush.Shape.Surfaces;
                        for (int idxBrushSurface = 0; idxBrushSurface < brushSurfaces.Length; idxBrushSurface++)
                        {
                            // TODO: add plane to this?
                            SelectedBrushSurface surfacePointer = new(brush, idxBrushSurface); 
                            surfaces[surfaceCount + idxBrushSurface] = surfacePointer;
                        }
                        surfaceCount += brush.Shape.Surfaces.Length;
                    }
                }

                var editModeMeshEdit = EditModeManager.ActiveTool as EditModeMeshEdit;
                if (editModeMeshEdit != null)
                {
                    var brushSelection = editModeMeshEdit.GetBrushSelection();
                    // Early out: No selection!
                    if (brushSelection == null) { return null; }

                    var sel_controlMeshes = brushSelection.ControlMeshes; 
                    var sel_controlMeshStates = brushSelection.States; 
                    var sel_shapes = brushSelection.Shapes;
                    var sel_brushes = brushSelection.Brushes;
                    if (sel_controlMeshStates.Length == 0) { return null; }

                    List<SelectedBrushSurface> builder = ListPool<SelectedBrushSurface>.Get(); // hacky but whatever
                    builder.Clear();
                    for (int idxSelection = 0; idxSelection < sel_controlMeshStates.Length; idxSelection++)
                    {
                        var controlMesh = sel_controlMeshes[idxSelection];
                        var polygonStates = sel_controlMeshStates[idxSelection].Selection.Polygons;
                        for (int idxPolygon = 0; idxPolygon < polygonStates.Length; idxPolygon++)
                        {
                            if ((polygonStates[idxPolygon] & SelectState.Selected) != 0)
                            {
                                int texGenIndex = controlMesh.Polygons[idxPolygon].TexGenIndex;
                                // found a selected polygon! look up the surface and add if we can
                                // TODO: replace this texgen based lookup with something less stupid
                                var lookupSurfaces = sel_shapes[idxSelection].Surfaces;
                                for (int idxSurface = 0; idxSurface < lookupSurfaces.Length; idxSurface++) {
                                    if (lookupSurfaces[idxSurface].TexGenIndex == texGenIndex) {
                                        // found it!
                                        builder.Add(new SelectedBrushSurface(sel_brushes[idxSelection], idxSurface)); // TODO: add plane?
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    surfaces = builder.ToArray(); // TODO: use a span + reusable buffer instead of re-allocating this every call?
                    ListPool<SelectedBrushSurface>.Release(builder);
                }
            }
            return surfaces;
        }
    }
}