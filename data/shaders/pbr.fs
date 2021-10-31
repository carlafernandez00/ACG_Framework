#define PI 3.14159265359
#define RECIPROCAL_PI 0.3183098861837697

uniform samplerCube u_skybox_texture;
uniform samplerCube u_texture_prem_0;
uniform samplerCube u_texture_prem_1;
uniform samplerCube u_texture_prem_2;
uniform samplerCube u_texture_prem_3;
uniform samplerCube u_texture_prem_4;

varying vec3 v_world_position;
varying vec2 v_uv;
varying vec3 v_normal;

uniform vec3 u_camera_position;
uniform vec3 u_light_pos;
uniform vec3 u_light_intensity;
uniform vec3 u_f0;
uniform float u_roughness_factor;
uniform vec3 u_color;

uniform sampler2D u_texture;
uniform sampler2D u_normal_texture;
uniform sampler2D u_rough_texture;
uniform sampler2D u_metal_texture;
uniform sampler2D u_2DLUT;
uniform sampler2D u_emissive;
uniform sampler2D u_opacity;
uniform bool u_use_metal;

const float GAMMA = 2.2;
const float INV_GAMMA = 1.0 / GAMMA;

struct PBRMat
{
	// properties
	float roughness;
	float metalness;
	vec3 albedo;
	vec3 F0;
	vec3 diffuseColor;
	float cosTheta;

	//extra
	vec3 emissive;
	float opacity;

	// vectors
	vec3 N;
	vec3 H;
	vec3 V;
	vec3 R;
	vec3 L;
	vec3 light;

	// dot
	float NdotL;
	float NdotV;
	float NdotH;
	float HdotL;
	float HdotV;
};

// degamma
vec3 gamma_to_linear(vec3 color)
{
	return pow(color, vec3(GAMMA));
}

// gamma
vec3 linear_to_gamma(vec3 color)
{
	return pow(color, vec3(INV_GAMMA));
}

vec3 getReflectionColor(vec3 r, float roughness)
{
	float lod = roughness * 5.0;

	vec4 color;

	if(lod < 1.0) color = mix( textureCube(u_skybox_texture, r), textureCube(u_texture_prem_0, r), lod );
	else if(lod < 2.0) color = mix( textureCube(u_texture_prem_0, r), textureCube(u_texture_prem_1, r), lod - 1.0 );
	else if(lod < 3.0) color = mix( textureCube(u_texture_prem_1, r), textureCube(u_texture_prem_2, r), lod - 2.0 );
	else if(lod < 4.0) color = mix( textureCube(u_texture_prem_2, r), textureCube(u_texture_prem_3, r), lod - 3.0 );
	else if(lod < 5.0) color = mix( textureCube(u_texture_prem_3, r), textureCube(u_texture_prem_4, r), lod - 4.0 );
	else color = textureCube(u_texture_prem_4, r);

	return color.rgb;
}

//Javi Agenjo Snipet for Bump Mapping
mat3 cotangent_frame(vec3 N, vec3 p, vec2 uv){
	// get edge vectors of the pixel triangle
	vec3 dp1 = dFdx( p );
	vec3 dp2 = dFdy( p );
	vec2 duv1 = dFdx( uv );
	vec2 duv2 = dFdy( uv );

	// solve the linear system
	vec3 dp2perp = cross( dp2, N );
	vec3 dp1perp = cross( N, dp1 );
	vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
	vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;

	// construct a scale-invariant frame
	float invmax = inversesqrt( max( dot(T,T), dot(B,B) ) );
	return mat3( T * invmax, B * invmax, N );
}

vec3 perturbNormal( vec3 N, vec3 V, vec2 texcoord, vec3 normal_pixel ){
	#ifdef USE_POINTS
	return N;
	#endif

	// assume N, the interpolated vertex normal and
	// V, the view vector (vertex to eye)
	//vec3 normal_pixel = texture2D(normalmap, texcoord ).xyz;
	normal_pixel = normal_pixel * 255./127. - 128./127.;
	mat3 TBN = cotangent_frame(N, V, texcoord);
	return normalize(TBN * normal_pixel);
}

vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 FresnelReflectance(float angle, vec3 F0, float roughness)
{
	return F0 + (1-F0)*pow(1-angle, 5.0);
}

/* 
	Convert 0-Inf range to 0-1 range so we can
	display info on screen
*/
vec3 toneMap(vec3 color)
{
    return color / (color + vec3(1.0));
}

// Uncharted 2 tone map
// see: http://filmicworlds.com/blog/filmic-tonemapping-operators/
vec3 toneMapUncharted2Impl(vec3 color)
{
    const float A = 0.15;
    const float B = 0.50;
    const float C = 0.10;
    const float D = 0.20;
    const float E = 0.02;
    const float F = 0.30;
    return ((color*(A*color+C*B)+D*E)/(color*(A*color+B)+D*F))-E/F;
}

vec3 toneMapUncharted(vec3 color)
{
    const float W = 11.2;
    color = toneMapUncharted2Impl(color * 2.0);
    vec3 whiteScale = 1.0 / toneMapUncharted2Impl(vec3(W));
    return color * whiteScale;
}

void computeVectors(inout PBRMat material){
	material.V = normalize(u_camera_position - v_world_position);
	material.L = normalize(u_light_pos - v_world_position);
	material.H = (material.V + material.L) / 2.0;
	// agafem els vectors normals de la textura->passem els valors de 0..1 a -1..1
	vec3 normal;
	normal = texture2D(u_normal_texture, v_uv).xyz;	
	material.N = perturbNormal( v_normal, material.V, v_uv, normal);
	material.R = normalize(reflect(-material.V, material.N)); 
}

void computeMaterialProperties(inout PBRMat material){
	material.albedo = gamma_to_linear(texture2D(u_texture, v_uv).xyz);	
	material.roughness = texture2D(u_rough_texture, v_uv).y * u_roughness_factor;
	material.roughness = clamp(material.roughness, 0.0 + 0.01, 1.0 - 0.01);
	if (u_use_metal) material.metalness = texture2D(u_metal_texture, v_uv).x;
	else material.metalness =  texture2D(u_rough_texture, v_uv).z;
	material.metalness = clamp(material.metalness, 0.0 + 0.01, 1.0 - 0.01);
	material.NdotL = clamp(dot(material.N, material.L), 0.0, 1.0);
	material.NdotV = clamp(dot(material.N, material.V), 0.0 + 0.01, 1.0 - 0.01);
	material.NdotH = clamp(dot(material.N, material.H), 0.0, 1.0);
	material.HdotL = clamp(dot(material.H, material.L), 0.0, 1.0);
	material.HdotV = clamp(dot(material.H, material.V), 0.0, 1.0);
	material.F0 = mix( u_f0, material.albedo * u_color, material.metalness ); //we compute the reflection in base to the color and the metalness
	material.diffuseColor = mix( vec3(0.0), material.albedo * u_color, material.metalness ); //we compute the reflection in base to the color and the metalness

	// extra
	material.emissive = gamma_to_linear(texture2D(u_emissive, v_uv).xyz);
	material.opacity = texture2D(u_opacity, v_uv).x;
}

float G1(float dot_product, float roughness){
	float k = pow(roughness + 1.0, 2.0) / 8.0;
	float g = (dot_product) / (dot_product * (1.0-k) + k);
	return g;
}

float Distribution(float NdotH,float roughness){
	float alpha2 = pow(roughness, 4.0);
	float d = (alpha2 * RECIPROCAL_PI) / (pow(pow(NdotH, 2.0) * (alpha2-1.0) + 1.0, 2.0));
	return d;
}

vec3 computeDirect(PBRMat material){
	vec3 direct;
	/// BSDF:
	// Diffuse component
	vec3 f_diffuse = material.diffuseColor * RECIPROCAL_PI;

	// Specular component
	vec3 F = FresnelReflectance(material.HdotL, material.F0, material.roughness);    					 //F
	float G = G1(material.HdotL, material.roughness)*G1(material.HdotV, material.roughness);		     //G
	float D = Distribution(material.NdotH, material.roughness); 		             					 //D

	vec3 f_specular = (F * G * D) / (4.0 * material.NdotL * material.NdotV + 1e-6);  

	vec3 bsdf = f_diffuse + f_specular;
	// Direct light
	direct = bsdf * material.light * material.NdotL;
	return direct;
}

vec3 computeIBL(PBRMat material){
	// IBL SPECULAR
	vec3 specularSample = getReflectionColor(material.R, material.roughness);
	vec3 Ks = FresnelSchlickRoughness(material.NdotV, material.F0, material.roughness);
	vec2 brdf2D = texture2D(u_2DLUT, vec2(material.NdotV, material.roughness)).xy;
	vec3 specularBRDF =  Ks * brdf2D.x + brdf2D.y;
	vec3 specularIBL = specularSample * specularBRDF;
	// IBL DIFFUSE
	vec3 diffuseSample = getReflectionColor(material.N, material.roughness);
	vec3 diffuseIBL = diffuseSample * material.diffuseColor * RECIPROCAL_PI;
	diffuseIBL *= (1 - Ks);
	return diffuseIBL + specularIBL;
}

vec4 getPixelColor(PBRMat material){
	vec3 direct = computeDirect(material);
	vec3 ibl = computeIBL(material);
	return vec4(direct + ibl + material.emissive, material.opacity);
}

void main()
{
	// 1. Create Material (struct, precalcular TOT)
	PBRMat material;

	// 2. Fill Material
	material.light = gamma_to_linear(u_light_intensity);
	computeVectors(material);
	computeMaterialProperties(material);

	// 3. Shade (Direct + Indirect)
	vec4 color = getPixelColor(material);

	// 4. Apply Tonemapping
	color.xyz = toneMapUncharted(color.xyz);

	// 5. Any extra texture to apply after tonemapping
	// ...

	// Last step: to gamma space
	color.xyz = linear_to_gamma(color.xyz);

	gl_FragColor = color;
}