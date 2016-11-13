float4x4 build_matrix_from_basis_vectors(
	float4 x_basis,
	float4 y_basis,
	float4 z_basis,
	float4 w_basis)
{
	return float4x4(
		x_basis.x, y_basis.x, z_basis.x, w_basis.x,
		x_basis.y, y_basis.y, z_basis.y, w_basis.y,
		x_basis.z, y_basis.z, z_basis.z, w_basis.z,
		x_basis.w, y_basis.w, z_basis.w, w_basis.w);
}

float4x4 build_transform_from_components(
	float3 position,
	float3 local_forward,
	float3 local_up,
	float3 local_right,
	float scale)
{
	return build_matrix_from_basis_vectors(
		float4((scale * local_right), 0.0f),
		float4((scale * local_up), 0.0f),
		float4((scale * local_forward), 0.0f),
		float4(position, 1.0f));
}

float4x4 build_transform_for_translation(
	float3 translation)
{
	return float4x4(
		1, 0, 0, translation.x,
		0, 1, 0, translation.y,
		0, 0, 1, translation.z,
		0, 0, 0, 1);
}

float4x4 build_transform_for_rotation_about_axis(
	float theta_radians,
	float3 axis) // The axis is assumed to already be normalized.
{
	// From: https://en.wikipedia.org/wiki/Rotation_matrix#Rotation_matrix_from_axis_and_angle

	float cos_theta = cos(theta_radians);
	float sin_theta = sin(theta_radians);

	float comp_cos_theta = (1.0f - cos_theta); // Create a shorthand for the complement, mostly for sanity-preservation.

	float4 x_basis = float4(
		(cos_theta + (axis.x * axis.x * comp_cos_theta)),
		((axis.y * axis.x * comp_cos_theta) + (axis.z * sin_theta)),
		((axis.z * axis.x * comp_cos_theta) - (axis.y * sin_theta)),
		0.0f);
	
	float4 y_basis = float4(
		((axis.x * axis.y * comp_cos_theta) - (axis.z * sin_theta)),
		(cos_theta + (axis.y * axis.y * comp_cos_theta)),
		((axis.z * axis.y * comp_cos_theta) + (axis.x * sin_theta)),
		0.0f);
	
	float4 z_basis = float4(
		((axis.x * axis.z * comp_cos_theta) + (axis.y * sin_theta)),
		((axis.y * axis.z * comp_cos_theta) - (axis.x * sin_theta)),
		(cos_theta + (axis.z * axis.z * comp_cos_theta)),
		0.0f);
	
	return build_matrix_from_basis_vectors(
		x_basis,
		y_basis,
		z_basis,
		float4(0, 0, 0, 1));
}

float4x4 build_transform_for_rotation_about_pivot(
	float theta_radians,
	float3 pivot_point,
	float3 pivot_axis) // The axis is assumed to already be normalized.
{
	// Translate the pivot to the origin, rotate, and translate back out to the pivot.
	return mul(
		build_transform_for_translation(pivot_point),
		mul(
			build_transform_for_rotation_about_axis(theta_radians, pivot_axis),
			build_transform_for_translation(-1.0f * pivot_point)));
}

float get_clamped_linear_fraction(
	float min,
	float max,
	float value)
{
	return saturate((value - min) / (max - min));
}

void ortho_normalize_basis_vectors(
	inout float3 local_forward,
	inout float3 inout_local_up,
	out float3 out_local_right)
{
	local_forward = normalize(local_forward);

	out_local_right = normalize(cross(inout_local_up, local_forward));

	inout_local_up = cross(local_forward, out_local_right);
}

float3 rotate_vector_about_axis_via_relative_ortho_normals(
	float3 input_vector,
	float theta_radians,
	float3 ortho_normal_alpha,
	float3 ortho_normal_bravo)
{
	// Project down into the plane orthogonal to the axis of rotation.
	float2 projection = float2(
		dot(input_vector, ortho_normal_alpha),
		dot(input_vector, ortho_normal_bravo));

	// Remember how much of the input vector is unaffected by the rotation.
	float3 input_invariant_component =
		input_vector -
		(
			(projection[0] * ortho_normal_alpha) +
			(projection[1] * ortho_normal_bravo)
		);

	// Rotate the projection.
	{
		float cos_theta = cos(theta_radians);
		float sin_theta = sin(theta_radians);

		projection = float2(
			(projection[0] * cos_theta) - (projection[1] * sin_theta),
			(projection[0] * sin_theta) + (projection[1] * cos_theta));
	}

	float3 result = (
		input_invariant_component +
		(projection[0] * ortho_normal_alpha) +
		(projection[1] * ortho_normal_bravo));

	return result;
}



