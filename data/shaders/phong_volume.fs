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

// Transfer Function
uniform sampler2D u_tf_text;
uniform bool u_use_tf;

uniform float u_iso_value;
uniform float u_h;

// material
uniform vec3 u_ka;
uniform vec3 u_kd;
uniform vec3 u_ks;
uniform float u_alpha;

// llum
uniform vec3 u_ia;
uniform vec3 u_id;
uniform vec3 u_is;
uniform vec3 u_light_pos;

vec3 computeGradient(vec3 sample_pos){
	vec3 grad_positions[6] = vec3[] (sample_pos + vec3(u_h, 0, 0),
						sample_pos - vec3(u_h, 0, 0),
						sample_pos + vec3(0, u_h, 0),
						sample_pos - vec3(0, u_h, 0),
						sample_pos + vec3(0, 0, u_h),
						sample_pos - vec3(0, 0, u_h)
	);
	vec3 gradient;
	for (int i = 0; i < 6; i+=2){
		gradient[int(i/2)] = texture3D(u_vol_text, (grad_positions[i] / 2.0 + vec3(0.5))).x;
		gradient[int(i/2)] -= texture3D(u_vol_text, (grad_positions[i+1] / 2.0 + vec3(0.5))).x;
	}
	return (1/(2*u_h)) * gradient;
}

vec4 computePhong(vec3 normal){
	vec3 ambient = u_ka * u_ia; // component ambient

	// component difusa
	vec3 N = normalize(normal);
	vec3 L = normalize(u_light_pos - v_world_position);
	float LdotN = clamp(dot(L,N), 0.0, 1.0);
	vec3 difuse = u_kd * LdotN * u_id;
	
	// component especular
	vec3 V = normalize(u_camera_position - v_world_position);
	vec3 R = normalize(reflect(-L, N));
	float RdotV = clamp(dot(R,V), 0.0, 1.0);
	vec3 specular = u_ks * pow(RdotV, u_alpha) * u_is;
	
	// total
	vec3 ip = ambient + difuse + specular;
	return vec4(ip * u_color * u_brightness, 1.0);
}

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
	vec3 grad, N;

	// Ray loop
	for(int i = 0; i<MAX_ITERATIONS; i++){
		// 2. Volume sampling
		d = texture3D(u_vol_text, (sample_pos / 2.0 + vec3(0.5))).x;
		if (d >= u_iso_value){

			grad = computeGradient(sample_pos);
			N = -normalize(grad);

			// 3. Classification
			// Transfer Function
			if (u_use_tf) sample_color = vec4(texture(u_tf_text, vec2(d, 1)).xyz, d);
			else sample_color = vec4(d);
			
			// 4. Composition
			sample_color.rgb *= sample_color.a;
			//color = sample_color;
			// color = vec4(N, 1.0); // Normal color
			color = computePhong(N);
			break;
		}

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