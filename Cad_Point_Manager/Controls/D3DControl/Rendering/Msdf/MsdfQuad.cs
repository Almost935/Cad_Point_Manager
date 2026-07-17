using SharpDX;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;

public static class MsdfQuad
{
    public static readonly MsdfVertex[] Vertices =
    {
        // Triangle 1
        new(-0.5f, -0.5f),
        new( 0.5f, -0.5f),
        new( 0.5f,  0.5f),

        // Triangle 2
        new(-0.5f, -0.5f),
        new( 0.5f,  0.5f),
        new(-0.5f,  0.5f),
    };
}