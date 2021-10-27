
varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;
varying vec4 v_color;

uniform sampler2D u_texture;
uniform vec3 u_camera_position; // camera

void main()
{
	vec3 albedo = texture2D(u_texture, v_uv).xyz;	
	
	gl_FragColor = vec4(albedo, 1.0) ;
	
}