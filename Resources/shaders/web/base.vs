precision mediump float;

// Vertex attributes
attribute vec3 vertexPosition;
attribute vec2 vertexTexCoord;
attribute vec3 vertexNormal;
attribute vec4 vertexColor;

// Uniforms
uniform mat4 mvp;

// Outputs to fragment shader
varying vec2 fragTexCoord;
varying vec4 fragColor;
varying vec3 fragNormal;

// NOTE: Add your custom variables here

void main()
{
    // Send attributes to fragment shader
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;
    fragNormal = vertexNormal;

    // Compute final clip-space position
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
