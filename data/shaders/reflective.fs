
varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;
varying vec4 v_color;

uniform samplerCube u_texture;
uniform vec3 u_camera_position; // camera

const float GAMMA = 2.2;
const float INV_GAMMA = 1.0 / GAMMA;
// degamma
vec3 gamma_to_linear(vec3 color){ return pow(color, vec3(GAMMA));}
// gamma
vec3 linear_to_gamma(vec3 color){ return pow(color, vec3(INV_GAMMA));}

void main()
{
	vec3 view = normalize(v_world_position-u_camera_position);	//calculem el vector direcció
	vec3 R = reflect(view, v_normal);
	vec4 environment = textureCube(u_texture, R);            //llegim la textura
	environment.xyz = linear_to_gamma(environment.xyz);

	gl_FragColor = environment;
}