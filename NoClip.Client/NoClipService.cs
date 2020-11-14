using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Communications;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Services;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using CitizenFX.Core;

namespace CroneFacade.NoClip.Client
{
	[PublicAPI]
	public class NoClipService : Service
	{
		public bool NoClipEnabled { get; private set; }
		public Vector3 NoClipPosition { get; private set; }

		// TODO: Move these settings to a config
		private float upPitchDivider = 70f;
		private float downPitchDivider = 35f;
		private float movementSpeed = 0.5f;
		private float movementSpeedInterval = 0.07f;
		private float movementSpeedBase = 0.5f;
		private float movementSpeedCap = 17f;
		private Control noClipHotKey = Control.VehicleHeadlight;
		private Control moveDownHotKey = Control.Cover;
		private Control moveUpHotKey = Control.Pickup;

		public NoClipService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user)
		{

		}

		public override async Task Started()
		{
			this.Ticks.On(OnTick);
		}

		private async Task NoClipTick()
		{
			Game.Player.Character.Position = this.NoClipPosition;

			// Change character heading to match camera heading
			Game.Player.Character.Heading += Game.Player.Character.Heading < 180f ? GameplayCamera.RelativeHeading + 180f : GameplayCamera.RelativeHeading - 180f;

			var newOffset = new Vector3(0f,0f,-1f);

			// Smooth acceleration / deceleration
			if (Game.IsControlPressed(1, Control.Sprint))
			{
				if (this.movementSpeed + this.movementSpeedInterval <= this.movementSpeedCap)
					this.movementSpeed += this.movementSpeedInterval;
			}
			else
			{
				if (this.movementSpeed - this.movementSpeedInterval * 2 >= this.movementSpeedBase)
					this.movementSpeed -= this.movementSpeedInterval * 2;
			}

			if (Game.IsControlPressed(1, Control.MoveUpOnly))
			{
				newOffset.Y += -this.movementSpeed;
				newOffset.Z += (GameplayCamera.RelativePitch < 0f ? GameplayCamera.RelativePitch / this.downPitchDivider : GameplayCamera.RelativePitch / this.upPitchDivider) * this.movementSpeed;
			}
			else if (Game.IsControlPressed(1, Control.MoveDownOnly))
			{
				newOffset.Y += this.movementSpeed;
				newOffset.Z += -(GameplayCamera.RelativePitch < 0f ? GameplayCamera.RelativePitch / this.downPitchDivider : GameplayCamera.RelativePitch / this.upPitchDivider) * this.movementSpeed;
			}

			if (Game.IsControlPressed(1, moveUpHotKey))
			{
				newOffset.Z += this.movementSpeed;
			}
			else if (Game.IsControlPressed(1, moveDownHotKey))
			{
				newOffset.Z += -this.movementSpeed;
			}

			if (newOffset.X != 0f || newOffset.Y != 0f || newOffset.Z != 0f)
				this.NoClipPosition = Game.Player.Character.GetOffsetPosition(newOffset);
		}

		private void ToggleNoClip()
		{
			NoClipEnabled = !NoClipEnabled;
			if (NoClipEnabled)
			{
				NoClipPosition = Game.Player.Character.Position;
				this.Ticks.On(NoClipTick);
				Game.Player.Character.IsInvincible = true;
			}
			else
			{
				this.Ticks.Off(NoClipTick);
				Game.Player.Character.IsInvincible = false;
			}
		}

		private async Task OnTick()
		{
			if (Game.IsControlJustReleased(1, this.noClipHotKey))
			{
				ToggleNoClip();
			}
		}
	}
}
