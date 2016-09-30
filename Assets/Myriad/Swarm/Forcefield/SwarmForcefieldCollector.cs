using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public interface ISwarmForcefieldAppender
{
	void AppendGlobalForcefield(
		Vector3 forceDirection, 
		float forceScalar);

	void AppendPlanarForcefield(
		Vector3 position, 
		Vector3 normal,
		float falloffInnerRadius,
		float falloffOuterRadius,
		float normalForceScalar);

	void AppendSphericalForcefield(
		Vector3 position,
		float falloffInnerRadius,
		float falloffOuterRadius,
		float outwardForceScalar);

	void AppendThrustCapsuleForcefield(
		Vector3 coreEndpointAlpha, 
		Vector3 coreEndpointBravo,
		float falloffInnerRadius,
		float falloffOuterRadius,
		float alphaToBravoForceScalar);
}

public class CollectingForcefieldsEventArgs : EventArgs
{
	public ISwarmForcefieldAppender ForcefieldAppender { get; set; }
}

public class SwarmForcefieldCollector : MonoBehaviour, ISwarmForcefieldAppender
{
	public static event EventHandler<CollectingForcefieldsEventArgs> CollectingForcefields;
	
	public void CollectForcefields(
		Matrix4x4 simulationToWorldMatrix,
		ref List<SwarmShaderForcefieldState> inoutForceFieldStates)
	{
		if (currentCollectionList != null)
		{
			throw new InvalidOperationException();
		}

		currentCollectionList = inoutForceFieldStates;
		currentSimulationToWorldMatrix = simulationToWorldMatrix;

		currentCollectionList.Clear();

		if (CollectingForcefields != null)
		{
			var eventArgs = new CollectingForcefieldsEventArgs();
			eventArgs.ForcefieldAppender = this;

			CollectingForcefields(this, eventArgs);
		}

		currentCollectionList = null;
		currentSimulationToWorldMatrix = Matrix4x4.identity;
	}
	
	void ISwarmForcefieldAppender.AppendGlobalForcefield(
		Vector3 forceDirection, 
		float forceScalar)
	{
		// The plane's forcefield-space places "position" at (0, 0, 0), and "position + noramlize(normal)" at (0, 1, 0).
		var forcefieldToWorldMatrix = 
			Matrix4x4.TRS(
				Vector3.zero,
				(Quaternion.LookRotation(forceDirection) * Quaternion.AngleAxis(90.0f, Vector3.right)),
				Vector3.one);

		AppendForcefield(
			forcefieldToWorldMatrix.inverse,
			SwarmShaderForcefieldState.ForcefieldTypeValues.Global,
			0.0f, // worldForcefieldLength
			1.0f, // worldFalloffInnerRadius (really just ignored)
			2.0f, // worldFalloffOuterRadius (really just ignored)
			forceScalar);
	}
	
	void ISwarmForcefieldAppender.AppendPlanarForcefield(
		Vector3 position, 
		Vector3 normal,
		float falloffInnerRadius,
		float falloffOuterRadius,
		float normalForceScalar)
	{
		// The plane's forcefield-space places "position" at (0, 0, 0), and "position + noramlize(normal)" at (0, 1, 0).
		var forcefieldToWorldMatrix = 
			Matrix4x4.TRS(
				position,
				(Quaternion.LookRotation(normal) * Quaternion.AngleAxis(90.0f, Vector3.right)),
				Vector3.one);

		AppendForcefield(
			forcefieldToWorldMatrix.inverse,
			SwarmShaderForcefieldState.ForcefieldTypeValues.Plane,
			0.0f, // worldForcefieldLength
			falloffInnerRadius,
			falloffOuterRadius,
			normalForceScalar);
	}
	
	void ISwarmForcefieldAppender.AppendSphericalForcefield(
		Vector3 position,
		float falloffInnerRadius,
		float falloffOuterRadius,
		float outwardForceScalar)
	{
		// The sphere's forcefield-space places "position" at (0, 0, 0).
		var forcefieldToWorldMatrix = 
			Matrix4x4.TRS(
				position,
				Quaternion.identity,
				Vector3.one);

		AppendForcefield(
			forcefieldToWorldMatrix.inverse,
			SwarmShaderForcefieldState.ForcefieldTypeValues.Sphere,
			0.0f, // worldForcefieldLength
			falloffInnerRadius,
			falloffOuterRadius,
			outwardForceScalar);
	}
	
	void ISwarmForcefieldAppender.AppendThrustCapsuleForcefield(
		Vector3 coreEndpointAlpha, 
		Vector3 coreEndpointBravo,
		float falloffInnerRadius,
		float falloffOuterRadius,
		float alphaToBravoForceScalar)
	{
		// The capsule's forcefield-space places "alpha" at (0, 0, 0), and "bravo" at (0, dist(alpha, bravo), 0).
		var forcefieldToWorldMatrix = 
			Matrix4x4.TRS(
				coreEndpointAlpha,
				(Quaternion.LookRotation(coreEndpointBravo - coreEndpointAlpha) * Quaternion.AngleAxis(90.0f, Vector3.right)),
				Vector3.one);

		AppendForcefield(
			forcefieldToWorldMatrix.inverse,
			SwarmShaderForcefieldState.ForcefieldTypeValues.ThrustCapsule,
			Vector3.Distance(coreEndpointAlpha, coreEndpointBravo), // worldForcefieldLength
			falloffInnerRadius,
			falloffOuterRadius,
			alphaToBravoForceScalar);
	}

	private List<SwarmShaderForcefieldState> currentCollectionList = null;
	private Matrix4x4 currentSimulationToWorldMatrix = Matrix4x4.identity;

	private void AppendForcefield(
		Matrix4x4 worldToForcefieldMatrix,
		SwarmShaderForcefieldState.ForcefieldTypeValues forcefieldType,
		float worldForcefieldLength,
		float worldFalloffInnerRadius,
		float worldFalloffOuterRadius,
		float worldForceScalar)
	{
		if (currentCollectionList == null)
		{
			throw new InvalidOperationException();
		}

		if (Mathf.Approximately(worldForceScalar, 0.0f) == false)
		{
			// NOTE: We're always assuming uniform-scaling.
			float simulationToWorldScale = currentSimulationToWorldMatrix.GetScale().x;
			
			float worldToSimulationScale = (1.0f / simulationToWorldScale);

			var forcefield = new SwarmShaderForcefieldState();

			forcefield.SimulationToForcefieldMatrix = 
				(worldToForcefieldMatrix * currentSimulationToWorldMatrix);
			
			forcefield.ForcefieldToSimulationMatrix = forcefield.SimulationToForcefieldMatrix.inverse;

			forcefield.ForcefieldType = (uint)forcefieldType;
			forcefield.ForcefieldLength = (worldForcefieldLength * worldToSimulationScale);

			// NOTE: Radius-corrections due to the simulation's size has already been 
			// factored in via currentSimulationToWorldMatrix.
			forcefield.FalloffInnerRadius = worldFalloffInnerRadius;
			forcefield.FalloffOuterRadius = worldFalloffOuterRadius;

			forcefield.ForceScalar = worldForceScalar;

			currentCollectionList.Add(forcefield);
		}
	}
}
