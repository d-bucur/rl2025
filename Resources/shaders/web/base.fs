precision mediump float;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

varying vec2 fragTexCoord;
varying vec4 fragColor;
varying vec3 fragNormal;

const vec3 lightDirection = vec3(255.0, 255.0, 255.0);
const float attenuation = 0.005;
const float minLight = 0.7;
const float maxLight = 3.0;

void main() {
    vec4 texelColor = texture2D(texture0, fragTexCoord);
    float light = dot(normalize(lightDirection), normalize(fragNormal));
    light = clamp(light * attenuation, minLight, maxLight);

    gl_FragColor = texelColor * colDiffuse * fragColor * vec4(vec3(light), 1.0);
}
