#ifndef SCENENODE_H
#define SCENENODE_H

#include "framework.h"

#include "shader.h"
#include "mesh.h"
#include "camera.h"
#include "material.h"

// Definim una classe pel node Light
class Light {
public:

	Light(std::string name);
	~Light();

	Vector3 difuse;
	Vector3 specular;
	Vector3 position;

	std::string name;

	virtual void renderInMenu();
	void setUniforms(Shader* shader);
};

class SceneNode {
public:

	static unsigned int lastNameId;

	SceneNode();
	SceneNode(const char* name);
	~SceneNode();

	Material * material = NULL;
	std::string name;

	Mesh* mesh = NULL;
	Matrix44 model;

	virtual void render(Camera* camera);
	virtual void renderWireframe(Camera* camera);
	virtual void renderInMenu();
};

// Definim una subclasse pel node Skybox
class SkyboxNode : public SceneNode{
public:

	SkyboxNode();
	SkyboxNode(const char* name);
	~SkyboxNode();

	virtual void renderInMenu();
};

#endif