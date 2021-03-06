﻿namespace SadConsole.Effects
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Switches between the character of a cell and a specified character for an amount of time, and then repeats.
    /// </summary>
    [DataContract]
    public class BlinkCharacter: CellEffectBase
    {
        [DataMember]
        private bool _isOn;
        [DataMember]
        private double _timeElapsed;

        /// <summary>
        /// In seconds, how fast the fade in and fade out each are
        /// </summary>
        [DataMember]
        public double BlinkSpeed { get; set; }

        /// <summary>
        /// The character index to blink into.
        /// </summary>
        [DataMember]
        public int CharacterIndex { get; set; }

        public BlinkCharacter()
        {
            BlinkSpeed = 1d;
            CharacterIndex = 0;
            _isOn = true;
            _timeElapsed = 0d;
            StartDelay = 0d;
        }

        #region ICellEffect Members

        public override void Apply(Cell cell)
        {
            if (!_isOn)
                cell.ActualCharacterIndex = CharacterIndex;
            else
                cell.ActualCharacterIndex = cell.CharacterIndex;
        }

        public override void Update(double gameTimeSeconds)
        {
            _timeElapsed += gameTimeSeconds;

            if (_delayFinished)
            {
                if (_timeElapsed >= BlinkSpeed)
                {
                    _isOn = !_isOn;
                    _timeElapsed = 0.0d;
                }
            }
            else
            {
                if (_timeElapsed >= _startDelay)
                {
                    _delayFinished = true;
                    _timeElapsed = 0d;
                }
            }
        }

        /// <summary>
        /// Restarts the cell effect but does not reset it.
        /// </summary>
        public override void Restart()
        {
            _timeElapsed = 0d;
            _isOn = true;

            base.Restart();
        }

        public override void Clear(Cell cell)
        {
            cell.CharacterIndex = cell.CharacterIndex;
        }

        public override ICellEffect Clone()
        {
            return new BlinkCharacter()
            {
                _isOn = this._isOn,
                _timeElapsed = this._timeElapsed,
                BlinkSpeed = this.BlinkSpeed,
                CharacterIndex = this.CharacterIndex,
                IsFinished = this.IsFinished,
                StartDelay = this.StartDelay,
                RemoveOnFinished = this.RemoveOnFinished,
                CloneOnApply = this.CloneOnApply
            };
        }

        public override bool Equals(ICellEffect effect)
        {
            if (effect is BlinkCharacter)
            {
                if (base.Equals(effect))
                {
                    var effect2 = (BlinkCharacter)effect;

                    return CharacterIndex == effect2.CharacterIndex &&
                           BlinkSpeed == effect2.BlinkSpeed &&
                           RemoveOnFinished == effect2.RemoveOnFinished &&
                           StartDelay == effect2.StartDelay;
                }
            }
            
            return false;
        }

        public override string ToString()
        {
            return string.Format("BLINKCHAR-{0}-{1}-{2}-{3}", CharacterIndex, BlinkSpeed, StartDelay, RemoveOnFinished);
        }
        #endregion
    }
}
