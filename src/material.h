#ifndef MATERIAL_H
#define MATERIAL_H

#include "framework.h"
#include "shader.h"
#include "camera.h"
#include "mesh.h"
#include "extra/hdre.h"

class Material {
public:

	Shader* shader = NULL;
	Texture* texture = NULL;
	vec4 color;

	virtual void setUniforms(Camera* camera, Matrix44 model) = 0;
	virtual void render(Mesh* mesh, Matrix44 model, Camera * camera) = 0;
	virtual void renderInMenu() = 0;
};

class StandardMaterial : public Material {
public:

	StandardMaterial();
	~StandardMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
	void render(Mesh* mesh, Matrix44 model, Camera * camera);
	void renderInMenu();
};

class TextureMaterial : public StandardMaterial {
public:

	TextureMaterial();
	~TextureMaterial();
};

// Definim una subclasse pel Phong 
class PhongMaterial : public Material {
public: 
	
	Texture* normal_texture = NULL;
	bool use_normal = false;	// Controlem si utilitzem una textura amb les normals 
	// Definim les variables de la llum
	Vector3 k_ambient;
	Vector3 k_difuse;
	Vector3 k_specular;
	float k_alpha;

	PhongMaterial();
	~PhongMaterial();
	
	void setUniforms(Camera* camera, Matrix44 model);
	void render(Mesh* mesh, Matrix44 model, Camera* camera);
	void renderInMenu();
	
};

class WireframeMaterial : public StandardMaterial {
public:

	WireframeMaterial();
	~WireframeMaterial();

	void render(Mesh* mesh, Matrix44 model, Camera * camera);
};

// Definim una subclasse pel material de la Skybox
class SkyboxMaterial : public Material {
public:

	SkyboxMaterial();
	~SkyboxMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
	void render(Mesh* mesh, Matrix44 model, Camera* camera);
	void renderInMenu();
};

// Definim una subclasse pel material reflectant
class ReflectiveMaterial : public Material {
public:

	ReflectiveMaterial();
	~ReflectiveMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
	void render(Mesh* mesh, Matrix44 model, Camera* camera);
	void renderInMenu();
};

// Definim una subclasse pel material PBR
class PBRMaterial : public StandardMaterial {
public:

	PBRMaterial();
	~PBRMaterial();

	Texture* albedo;
	Texture* normal;
	Texture* roughness;
	Texture* metalness;
	Texture* emissive;
	Texture* opacity;
	Texture* brdfLUT;
	Texture* prem_0;
	Texture* prem_1;
	Texture* prem_2;
	Texture* prem_3;
	Texture* prem_4;
	
	Vector3 f0;
	bool use_metal;
	float roughness_factor;

	void setUniforms(Camera* camera, Matrix44 model);
	void render(Mesh* mesh, Matrix44 model, Camera* camera);
	void renderInMenu();
};

#endif