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

float4x4 build_transform_from_components(
	float3 position,
	float3 local_forward,
	float3 local_up,
	float3 local_right,
	float scale)
{
	return build_matrix_from_columns(
		float4((scale * local_right), 0.0f),
		float4((scale * local_up), 0.0f),
		float4((scale * local_forward), 0.0f),
		float4(position, 1.0f));
}

void ortho_normalize_basis_vectors(
	float3 local_forward, // NOTE: Assumed to already be normalized.
	inout float3 inout_local_up,
	out float3 out_local_right)
{
	out_local_right = normalize(cross(inout_local_up, local_forward));

	inout_local_up = cross(local_forward, out_local_right);
}
