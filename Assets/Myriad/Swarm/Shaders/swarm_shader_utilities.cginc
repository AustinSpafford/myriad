float4x4 build_matrix_from_columns(
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

float4x4 build_swarmer_model_to_swarm_matrix(
	float3 position,
	float3 velocity,
	float3 local_up,
	float scale)
{
	// Keep the nose of the swarmer pointed in its direction of motion.
	float3 swarmer_forward = normalize(velocity);

	// Set the swarmer's roll to respect its desired "down".
	float3 swarmer_right = normalize(cross(local_up, swarmer_forward));

	// The corrected up-vector is now strictly implied.
	float3 swarmer_up = cross(swarmer_forward, swarmer_right);

	return build_matrix_from_columns(
		float4((scale * swarmer_right), 0.0f),
		float4((scale * swarmer_up), 0.0f),
		float4((scale * swarmer_forward), 0.0f),
		float4(position, 1.0f));
}
