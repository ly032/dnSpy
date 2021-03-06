/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class SpaceReservationStack : ISpaceReservationStack {
		public bool HasAggregateFocus { get; private set; }
		public event EventHandler? GotAggregateFocus;
		public event EventHandler? LostAggregateFocus;

		public bool IsMouseOver {
			get {
				foreach (var mgr in SpaceReservationManagers) {
					if (mgr.IsMouseOver)
						return true;
				}
				return false;
			}
		}

		IEnumerable<SpaceReservationManager> SpaceReservationManagers {
			get {
				foreach (var mgr in spaceReservationManagers) {
					if (!(mgr is null))
						yield return mgr;
				}
			}
		}

		readonly IWpfTextView wpfTextView;
		readonly string[] spaceReservationManagerNames;
		readonly SpaceReservationManager?[] spaceReservationManagers;

		public SpaceReservationStack(IWpfTextView wpfTextView, string[] spaceReservationManagerNames) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			this.spaceReservationManagerNames = spaceReservationManagerNames ?? throw new ArgumentNullException(nameof(spaceReservationManagerNames));
			spaceReservationManagers = new SpaceReservationManager[spaceReservationManagerNames.Length];
			wpfTextView.Closed += WpfTextView_Closed;
		}

		int GetNameIndex(string name) {
			for (int i = 0; i < spaceReservationManagerNames.Length; i++) {
				if (spaceReservationManagerNames[i] == name)
					return i;
			}
			return -1;
		}

		public ISpaceReservationManager GetSpaceReservationManager(string name) {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			int index = GetNameIndex(name);
			if (index < 0)
				throw new ArgumentException();
			var mgr = spaceReservationManagers[index];
			if (mgr is null) {
				mgr = new SpaceReservationManager(wpfTextView);
				mgr.GotAggregateFocus += SpaceReservationManager_GotAggregateFocus;
				mgr.LostAggregateFocus += SpaceReservationManager_LostAggregateFocus;
				spaceReservationManagers[index] = mgr;
			}
			return mgr;
		}

		void SpaceReservationManager_GotAggregateFocus(object? sender, EventArgs e) => UpdateAggregateFocus();
		void SpaceReservationManager_LostAggregateFocus(object? sender, EventArgs e) => UpdateAggregateFocus();

		void UpdateAggregateFocus() {
			if (wpfTextView.IsClosed)
				return;
			bool newValue = CalculateAggregateFocus();
			if (newValue != HasAggregateFocus) {
				HasAggregateFocus = newValue;
				if (newValue)
					GotAggregateFocus?.Invoke(this, EventArgs.Empty);
				else
					LostAggregateFocus?.Invoke(this, EventArgs.Empty);
			}
		}

		bool CalculateAggregateFocus() {
			foreach (var mgr in SpaceReservationManagers) {
				if (mgr.HasAggregateFocus)
					return true;
			}
			return false;
		}

		public void Refresh() {
			if (wpfTextView.IsClosed)
				return;
			GeometryGroup? geometry = null;
			foreach (var mgr in SpaceReservationManagers) {
				if (geometry is null)
					geometry = new GeometryGroup();
				mgr.PositionAndDisplay(geometry);
			}
		}

		void WpfTextView_Closed(object? sender, EventArgs e) {
			wpfTextView.Closed -= WpfTextView_Closed;
			for (int i = 0; i < spaceReservationManagers.Length; i++) {
				var mgr = spaceReservationManagers[i];
				if (!(mgr is null)) {
					spaceReservationManagers[i] = null;
					mgr.GotAggregateFocus -= SpaceReservationManager_GotAggregateFocus;
					mgr.LostAggregateFocus -= SpaceReservationManager_LostAggregateFocus;
				}
			}
		}
	}
}
