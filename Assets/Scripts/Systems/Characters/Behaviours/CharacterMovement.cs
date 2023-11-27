using System;
using Systems.Core.Characters.Behaviours;
using UnityEngine;
using Zenject;

namespace Systems.Characters.Behaviours
{
    public class CharacterMovement : IMovement, IFixedTickable
    {
        public Vector2 Position => _references.transformHandler.position;
        public bool IsGrounded => Physics2D.OverlapCircle(_references.feetPoint.position, 0.2f, _walkableLayerMask);
        public bool IsFacingRight { get; private set; } = true;
        
        private readonly References _references;
        private readonly Settings _settings;

        private float _horizontalMove;

        private readonly int _walkableLayerMask = LayerMask.GetMask("Walkable");

        [Inject]
        public CharacterMovement(References references, Settings settings)
        {
            _references = references;
            _settings = settings;
        }

        public void FixedTick()
        {
            var movement = CalculateMovement();
            ApplyForce(Vector2.right, movement, ForceMode2D.Force);

            var friction = CalculateFriction();
            ApplyForce(Vector2.left, friction, ForceMode2D.Impulse);
        }

        private float CalculateMovement()
        {
            var targetSpeed = _horizontalMove * _settings.speed;
            var speedDifference = targetSpeed - _references.rigidbody.velocity.y;

            var accelerationRate = (Mathf.Abs(targetSpeed) > 0.01f)
                ? _settings.acceleration
                : _settings.decceleration;

            var direction = Mathf.Sign(speedDifference);
            var force = Mathf.Abs(speedDifference) * accelerationRate;
            var movement = Mathf.Pow(force, _settings.velocityPower) * direction;
            
            return movement;
        }

        private float CalculateFriction()
        {
            if (!IsGrounded || Mathf.Abs(_horizontalMove) >= 0.01f)
                return 0;

            var amount = Mathf.Min(Mathf.Abs(_references.rigidbody.velocity.x), Mathf.Abs(_settings.frictionAmount));
            amount *= Mathf.Sign(_references.rigidbody.velocity.x);

            return amount;
        }

        public void ApplyForce(Vector2 direction, float force, ForceMode2D mode)
        {
            if (force == 0)
                return;
            
            _references.rigidbody.AddForce(direction * force, mode);
        }

        public void PerformMove(Vector2 delta)
        {
            _horizontalMove = delta.x;
            
            if (ShouldBeFlipped())
                Flip();
        }

        public void PerformJump()
        {
            if (!IsGrounded)
                return;
            
            var force = _settings.jumpingPower;
            var currentVerticalVelocity = _references.rigidbody.velocity.y;
            if (currentVerticalVelocity < 0)
                force -= currentVerticalVelocity;

            ApplyForce(Vector2.up, force, ForceMode2D.Impulse);
        }
        
        private void Flip()
        {
            IsFacingRight = !IsFacingRight;

            var localScale = _references.transformHandler.localScale;
            localScale.x *= -1f;

            _references.transformHandler.localScale = localScale;
        }
        
        private bool ShouldBeFlipped()
        {
            return IsFacingRight && _horizontalMove < 0f || !IsFacingRight && _horizontalMove > 0f;
        }

        [Serializable]
        public class References
        {
            public Transform transformHandler;
            public Rigidbody2D rigidbody;
            public Transform feetPoint;
        }

        [Serializable]
        public class Settings
        {
            public float speed = 20f;
            public float jumpingPower = 2f;
            
            public float acceleration = 13;
            public float decceleration = 16;

            public float velocityPower = 0.96f;
            public float frictionAmount = 2.5f;
        }
    }
}