#include "material.h"
#include "texture.h"
#include "application.h"
#include "extra/hdre.h"

StandardMaterial::StandardMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/flat.fs");
}

StandardMaterial::~StandardMaterial()
{

}

void StandardMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);
	shader->setUniform("u_time", Application::instance->time);
	shader->setUniform("u_output", Application::instance->output);

	shader->setUniform("u_color", color);
	shader->setUniform("u_exposure", Application::instance->scene_exposure);

	if (texture)
		shader->setUniform("u_texture", texture);
}

void StandardMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		//disable shader
		shader->disable();
	}
}

void StandardMaterial::renderInMenu()
{
	//ImGui::ColorEdit3("Color", (float*)&color); // Edit 3 floats representing a color

}

WireframeMaterial::WireframeMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/flat.fs");
}

WireframeMaterial::~WireframeMaterial()
{

}

void WireframeMaterial::render(Mesh* mesh, Matrix44 model, Camera * camera)
{
	if (shader && mesh)
	{
		glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);

		//enable shader
		shader->enable();

		//upload material specific uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	}
}

TextureMaterial::TextureMaterial()
{
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/texture.fs");
}

TextureMaterial::~TextureMaterial()
{
}

PhongMaterial::PhongMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/phong.fs");

}

PhongMaterial::~PhongMaterial()
{
}

void PhongMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);
	shader->setUniform("u_time", Application::instance->time);
	shader->setUniform("u_output", Application::instance->output);

	shader->setUniform("u_color", color);
	shader->setUniform("u_exposure", Application::instance->scene_exposure);
	// material
	shader->setUniform("u_ka", k_ambient);
	shader->setUniform("u_kd", k_difuse);
	shader->setUniform("u_ks", k_specular);
	shader->setUniform("u_alpha", k_alpha);

	// Ambient light
	shader->setUniform("u_ia", Application::instance->ambient_light);

	if (texture)
		shader->setUniform("u_texture", texture);

	if (normal_texture) {
		shader->setUniform("u_normal_texture", normal_texture);
		use_normal = true;
	}
	shader->setUniform("u_use_normal", use_normal);
}

void PhongMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);
	
		// Fem un for per afegir cada llum a l'escena
		for (int i = 0; i < Application::instance->light_list.size(); i++) {
			if (i == 1) {
				//Habilitem el blending
				glEnable(GL_BLEND);
				glBlendFunc(GL_SRC_ALPHA, GL_ONE);
				glDepthFunc(GL_LEQUAL);
				
				shader->setUniform("u_ia", Vector3(0,0,0));
			}

			Application::instance->light_list[i]->setUniforms(shader);
			
			//do the draw call
			mesh->render(GL_TRIANGLES);
		}
		
		// Deshabilitem el blending
		glDisable(GL_BLEND);
		glDepthFunc(GL_LESS); //as default

		//disable shader
		shader->disable();
	}
}

void PhongMaterial::renderInMenu()
{
	// creem sliders per modificar les constants del material
	ImGui::DragFloat3("Ka", k_ambient.v, 0.1f, 0.0, 1.0);  //definim un rang
	ImGui::DragFloat3("Kd", k_difuse.v, 0.1f, 0.0, 1.0);   //definim un rang
	ImGui::DragFloat3("Ks", k_specular.v, 0.1f, 0.0, 1.0); //definim un rang
	ImGui::SliderFloat("Alpha", &k_alpha, 0.1f, 500.0f);   //definim un rang
}

SkyboxMaterial::SkyboxMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/skybox.fs");
}

SkyboxMaterial::~SkyboxMaterial()
{
}

void SkyboxMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_model", model);
	
	shader->setUniform("u_camera_position", camera->eye);

	if (texture)
		shader->setUniform("u_texture", texture);
}

void SkyboxMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);
		
		glDisable(GL_DEPTH_TEST);
		
		//do the draw call
		mesh->render(GL_TRIANGLES);
		
		glEnable(GL_DEPTH_TEST);

		//disable shader
		shader->disable();
	}
}

void SkyboxMaterial::renderInMenu()
{
	ImGui::ColorEdit3("Color", (float*)&color); // Edit 3 floats representing a color
}


ReflectiveMaterial::ReflectiveMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/reflective.fs"); 
}

ReflectiveMaterial::~ReflectiveMaterial()
{
}

void ReflectiveMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_model", model);

	shader->setUniform("u_camera_position", camera->eye);

	if (texture)
		shader->setUniform("u_texture", texture);
}

void ReflectiveMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		//disable shader
		shader->disable();
	}
}

void ReflectiveMaterial::renderInMenu()
{
}

PBRMaterial::PBRMaterial()
{
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/pbr.fs");
}

PBRMaterial::~PBRMaterial()
{
}

void PBRMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);
	shader->setUniform("u_time", Application::instance->time);
	shader->setUniform("u_output", Application::instance->output);

	shader->setUniform("u_exposure", Application::instance->scene_exposure);
	// material
	if (albedo) shader->setUniform("u_texture", albedo, 0);
	if (normal) shader->setUniform("u_normal_texture", normal, 1);
	if (roughness) shader->setUniform("u_rough_texture", roughness, 2);
	if (metalness) shader->setUniform("u_metal_texture", metalness, 3);

	shader->setUniform("u_light_pos", Application::instance->light_list[0]->position);
	shader->setUniform("u_light_intensity", Application::instance->light_list[0]->difuse);
}
