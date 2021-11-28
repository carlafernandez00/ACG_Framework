#include "scenenode.h"
#include "application.h"
#include "texture.h"
#include "utils.h"

unsigned int SceneNode::lastNameId = 0;
unsigned int mesh_selected = 0;
unsigned int skybox_selected = 0;
unsigned int material_selected = 0;

SceneNode::SceneNode()
{
	this->name = std::string("Node" + std::to_string(lastNameId++));
	visible = true;
}


SceneNode::SceneNode(const char * name)
{
	this->name = name;
	visible = true;
}

SceneNode::~SceneNode()
{

}

void SceneNode::render(Camera* camera)
{
	if (material)
		material->render(mesh, model, camera);
}

void SceneNode::renderWireframe(Camera* camera)
{
	WireframeMaterial mat = WireframeMaterial();
	mat.render(mesh, model, camera);
}

void SceneNode::renderInMenu()
{
	ImGui::Checkbox("Visible", &visible);
	//Model edit
	if (ImGui::TreeNode("Model")) 
	{
		float matrixTranslation[3], matrixRotation[3], matrixScale[3];
		ImGuizmo::DecomposeMatrixToComponents(model.m, matrixTranslation, matrixRotation, matrixScale);
		ImGui::DragFloat3("Position", matrixTranslation, 0.1f);
		ImGui::DragFloat3("Rotation", matrixRotation, 0.1f);
		ImGui::DragFloat3("Scale", matrixScale, 0.1f);
		ImGuizmo::RecomposeMatrixFromComponents(matrixTranslation, matrixRotation, matrixScale, model.m);
		
		ImGui::TreePop();
	}

	//Material
	if (material && ImGui::TreeNode("Material"))
	{
		// Permet canviar el material
		bool mat_changed = false;
		mat_changed |= ImGui::Combo("Material", (int*)&material_selected, "BASIC\0PHONG\0");
		// Assignem una malla i textura diferent segons la opció escollida
		if (mat_changed) {
			Texture* aux_text = this->material->texture;
			switch (material_selected) {
			case 0: material = new VolumeMaterial(); break;
			case 1: material = new IsoVolumeMaterial(); break;
			}
			material->texture = aux_text;
		}
		material->renderInMenu();
		ImGui::TreePop();
		
	}

	//Geometry
	if (mesh && ImGui::TreeNode("Geometry"))
	{
		// Permet canviar la mesh 
		bool changed = false;
		changed |= ImGui::Combo("Mesh", (int*)&mesh_selected, "SPHERE\0HELMET\0BENCH\0CUBE\0");
		// Assignem una malla i textura diferent segons la opció escollida
		if (changed) {
			switch (mesh_selected){
			case 0: mesh = Mesh::Get("data/meshes/sphere.obj.mbin"); 
				material->texture = Texture::Get("data/models/ball/albedo.png");
				model.modifyScale(1.0, 1.0, 1.0); //Reescalem la malla perqu? tingui el tamany desitjat
				break;
			case 1: mesh = Mesh::Get("data/models/helmet/helmet.obj.mbin"); 
				material->texture = Texture::Get("data/models/helmet/albedo.png"); 
				model.modifyScale(1.0, 1.0, 1.0); //Reescalem la malla perqu? tingui el tamany desitjat
				break;
			case 2: mesh = Mesh::Get("data/models/bench/bench.obj.mbin"); 
				material->texture = Texture::Get("data/models/bench/albedo.png"); 
				model.modifyScale(1.8, 1.8, 1.8); //Reescalem la malla perqu? tingui el tamany desitjat
				break;
			case 3:
				mesh = Mesh::getCube();
			}
		}

		ImGui::TreePop();
	}
}

Light::Light(std::string name)
{
	this->name = name;
	visible = true;
}

void Light::renderInMenu()
{
	ImGui::DragFloat3("Position", position.v, 0.1f);  //Slider per moure la posicio de la llum
	ImGui::ColorEdit3("Difuse Color", difuse.v);      //Permet modificar la llum difusa
	ImGui::ColorEdit3("Specular Color", specular.v);  //Permet modificar la llum especular
	ImGui::Checkbox("Visible", &visible);
	
}

void Light::setUniforms(Shader* shader)
{
	shader->setUniform("u_id", difuse);
	shader->setUniform("u_is", specular);
	shader->setUniform("u_light_pos", position);

}

SkyboxNode::SkyboxNode()
{
	this->name = std::string("Node" + std::to_string(lastNameId++));
}

SkyboxNode::SkyboxNode(const char* name)
{
	this->name = name;
	visible = true;
}

SkyboxNode::~SkyboxNode()
{
}


void SkyboxNode::renderInMenu()
{
	ImGui::Checkbox("Visible", &visible);
	// Definim tres opcions de fons
	bool changed = false;
	changed |= ImGui::Combo("Skybox", (int*)&skybox_selected, "CITY\0SNOW\0DRAGONVALE\0");
	// Assignem una textura diferent al skybox segons la opci? escollida
	if (changed) {
		switch (skybox_selected) {
		case 0: material->texture->cubemapFromImages("data/environments/city"); break;
		case 1: material->texture->cubemapFromImages("data/environments/snow"); break;
		case 2: material->texture->cubemapFromImages("data/environments/dragonvale"); break;
		}
	}
}
