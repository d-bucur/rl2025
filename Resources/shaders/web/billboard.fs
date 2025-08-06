#version 100   
#extension GL_EXT_frag_depth : enable 
precision mediump float;

// Inputs from vertex shader
varying vec2 fragTexCoord;
varying vec4 fragColor;

// Uniforms
uniform sampler2D texture0;
uniform vec4 colDiffuse;

void main()
{
    vec4 texelColor = texture2D(texture0, fragTexCoord);

    if (texelColor.a <= 0.0) {
        discard;
    }

    gl_FragColor = texelColor * colDiffuse * fragColor;
    gl_FragDepthEXT = gl_FragCoord.z - 0.5;
}
