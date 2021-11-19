#define MAX_ITERATIONS 80

varying vec3 v_position;
varying vec3 v_world_position;

uniform vec3 u_camera_position;
uniform sampler3D u_vol_text;
uniform mat4 u_iModel;
uniform float u_step;

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

	// Ray loop
	for(int i = 0; i<MAX_ITERATIONS; i++){
		// 2. Volume sampling
		d = texture(u_vol_text, (sample_pos / 2.0 + vec3(0.5))).x;

		// 3. Classification
		//sample_color = vec4(d);
		sample_color = vec4(d,1-d,0,d*d);

		// 4. Composition
		sample_color.rgb *= sample_color.a;
		color += u_step * (1.0 - color.a)*sample_color;

		// 5. Next sample
		sample_pos += direction.xyz;

		// 6. Early termination
		if (color.a >= 1.0) break;
		if (any(greaterThan(sample_pos.xyz, vec3(1.0))) || any(lessThan(sample_pos.xyz, vec3(-1.0)))) break;
		
	}

	//7. Final color	
	gl_FragColor = color;

}