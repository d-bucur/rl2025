#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;

// Input uniform values
uniform sampler2D texture0;
uniform vec4 colDiffuse;

// Output fragment color
out vec4 finalColor;

// NOTE: Add your custom variables here
vec3 lightDirection = vec3(255, 255, 255);
float attenuation = 0.005;
float minLight = 0.7;
float maxLight = 3;

void main()
{
    // Texel color fetching from texture sampler
    vec4 texelColor = texture(texture0, fragTexCoord);

    // NOTE: Implement here your fragment shader code

    // final color is the color from the texture 
    //    times the tint color (colDiffuse)
    //    times the fragment color (interpolated vertex color)
    
    float light = min(max(dot(lightDirection, fragNormal) * attenuation, minLight), maxLight);
    finalColor = texelColor*colDiffuse*fragColor * vec4(vec3(light), 1.0);
}

