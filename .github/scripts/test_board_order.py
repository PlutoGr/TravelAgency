"""Smoke: hygiene board_order imports and pure helpers (no network)."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import board_order as bo


class HygieneHelpers(unittest.TestCase):
    def test_queue_key_priority_first(self):
        a = {"prio": 2, "number": 10}
        b = {"prio": None, "number": 1}
        self.assertLess(bo.queue_key(a), bo.queue_key(b))

    def test_desired_order_queue(self):
        items = [
            {"number": 3, "prio": None, "closedAt": None},
            {"number": 1, "prio": 2, "closedAt": None},
            {"number": 2, "prio": 1, "closedAt": None},
        ]
        d = bo.desired_order("Очередь", items)
        self.assertEqual([i["number"] for i in d], [2, 1, 3])

    def test_managed(self):
        self.assertTrue(bo.managed({"type": "ISSUE", "archived": False, "number": 1}))
        self.assertFalse(bo.managed({"type": "PULL_REQUEST", "archived": False, "number": 1}))

    def test_docstring_forbids_status_moves(self):
        doc = bo.__doc__ or ""
        self.assertIn("НЕ двигает стадии", doc)
        self.assertIn("Метки issues не меняются", doc)


if __name__ == "__main__":
    unittest.main()
