uint3 spatialization_wrap_voxel_coord(
	int3 unbounded_voxel_coord,
	uint voxel_count_per_axis)
{
	// Which wrapping of the voxel-grid are we in?
	int3 macro_voxel_coordinate = (unbounded_voxel_coord / voxel_count_per_axis);

	// Determine our position within the macro-voxel. 
	// Note that this is different than using truncation, because we want -1 and 1 to map to
	// different values (to avoid excessive collisions around the origin).
	uint3 wrapped_voxel_coord = (
		unbounded_voxel_coord - 
		(macro_voxel_coordinate * voxel_count_per_axis));

	return wrapped_voxel_coord;
}

uint spatialization_get_voxel_index_from_voxel_coord(
	uint3 wrapped_voxel_coord,
	uint voxel_count_per_axis)
{
	return (
		wrapped_voxel_coord.x +
		(wrapped_voxel_coord.y * voxel_count_per_axis) +
		(wrapped_voxel_coord.z * (voxel_count_per_axis * voxel_count_per_axis)));
}

uint spatialization_get_neighborhood_enumeration_voxel_index(
	uint3 neighborhood_min_voxel_coord,
	uint voxel_count_per_axis,
	inout int inout_neighborhood_enumeration_index) // Start at 0, and this will become -1 when the enumeration has completed.
{
	uint result = (uint)-1;

	if (inout_neighborhood_enumeration_index < 8)
	{
		// Generate an offset that permutes into all 8 voxels within the neighborhood.
		uint3 enumeration_voxel_offset = uint3(
			((inout_neighborhood_enumeration_index & (1 << 0)) != 0),
			((inout_neighborhood_enumeration_index & (1 << 1)) != 0),
			((inout_neighborhood_enumeration_index & (1 << 2)) != 0));

		// Index into the neighborhood, making sure to wrap the coordinates so
		// neighborhoods that span the extremes of each axis are handled correctly.
		result = 
			spatialization_get_voxel_index_from_voxel_coord(
				spatialization_wrap_voxel_coord(
					(neighborhood_min_voxel_coord + enumeration_voxel_offset),
					voxel_count_per_axis),
				voxel_count_per_axis);

		++inout_neighborhood_enumeration_index;
	}
	else
	{
		// Signal that the enumeration has completed.
		inout_neighborhood_enumeration_index = -1;
	}

	return result;
}
