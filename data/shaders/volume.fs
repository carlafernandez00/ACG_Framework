#define MAX_ITERATIONS 100000

varying vec3 v_position;
varying vec3 v_world_position;
varying vec2 v_uv;

uniform vec3 u_camera_position;
uniform sampler3D u_vol_text;
uniform mat4 u_iModel;
uniform float u_step;
uniform float u_brightness;
uniform vec4 u_color;
// Jittering
uniform sampler2D u_noise_text;
uniform bool u_use_jittering;
uniform float u_texture_width;
// Transfer Function
uniform sampler2D u_tf_text;
uniform bool u_use_tf;
// Clipping
uniform bool u_use_clipping;
uniform vec4 u_plane;

void main(){
	// 1. Ray setup
	vec3 dir = normalize(v_world_position - u_camera_position);
	vec4 direction = (u_iModel * vec4(dir, 1.0)); // to local coordinates
	direction /= direction.w;
	direction *= u_step;
	vec3 sample_pos = v_position;
	vec4 color = vec4(0.0);
	vec4 sample_color;
	float d;
	float clipping_val;

	// Jittering
	if (u_use_jittering){
		float random_offset = texture(u_noise_text, gl_FragCoord.xy / u_texture_width).x;
		sample_pos += random_offset * direction;
	}

	// Ray loop
	for(int i = 0; i<MAX_ITERATIONS; i++){
		// 2. Volume sampling
		d = texture3D(u_vol_text, (sample_pos / 2.0 + vec3(0.5))).x;

		// 3. Classification
		// Transfer Function
		if (u_use_tf) sample_color = vec4(texture(u_tf_text, vec2(d, 1)).xyz, d);
		else sample_color = vec4(d);
		
		// 4. Composition
		if (u_use_clipping){
			clipping_val = dot(u_plane, vec4(sample_pos.xyz, 1));
			if (clipping_val >= 0) sample_color = vec4(0.0);
		}
		sample_color.rgb *= sample_color.a;
		color += u_step * (1.0 - color.a)*sample_color;

		// 5. Next sample
		sample_pos += direction.xyz;

		// 6. Early termination
		if (color.a >= 1.0) break;
		if (any(greaterThan(sample_pos.xyz, vec3(1.0))) || any(lessThan(sample_pos.xyz, vec3(-1.0)))) break;
		
	}

	if (color.a  <= 0.01) discard;

	//7. Final color	
	gl_FragColor = u_brightness * u_color * color;

}