"""Тесты чистых функций board_order.py (без сети и без токена)."""
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import board_order as bo  # noqa: E402


def issue(number, labels, state="OPEN", status=None, prio=None, body1=""):
    return {"number": number, "title": f"t{number}", "state": state, "closedAt": None,
            "labels": list(labels), "body1": body1, "id": f"i{number}", "type": "ISSUE",
            "archived": False, "status": status, "prio": prio}


class BoardOrderTest(unittest.TestCase):
    def setUp(self):
        bo.OPTIONS = {c: f"o-{c}" for c in bo.REQUIRED_COLUMNS}

    def test_hold_prefix_alone_does_not_move_or_relabel(self):
        it = issue(1, ["статус: разработка"], status="Разработка", body1="⏸ На удержании: ждём")
        notes, conflicts = [], []
        self.assertEqual(bo.wanted_status(it, notes, conflicts), "Разработка")
        self.assertEqual(conflicts, [])
        self.assertTrue(any("префикс" in n for n in notes))
        self.assertEqual(bo.plan_label_fixes([it], []), [])

    def test_hold_prefix_without_label_gets_queue_label(self):
        it = issue(2, [], body1="⏸ На удержании")
        fixes = bo.plan_label_fixes([it], [])
        self.assertEqual(len(fixes), 1)
        self.assertEqual(fixes[0][1], bo.QUEUE_LABEL)

    def test_open_done_is_left_and_reported(self):
        it = issue(3, ["статус: готово"], status="Удержание")
        self.assertEqual(bo.plan_label_fixes([it], []), [])
        conflicts = []
        self.assertIsNone(bo.wanted_status(it, [], conflicts))
        self.assertEqual(len(conflicts), 1)

    def test_closed_gets_done(self):
        it = issue(4, ["статус: приёмка"], state="CLOSED", status="Приёмка")
        fixes = bo.plan_label_fixes([it], [])
        self.assertEqual(fixes[0][1], bo.DONE_LABEL)
        self.assertEqual(fixes[0][2], ["статус: приёмка"])
        self.assertEqual(bo.wanted_status(it, []), "Готово")

    def test_two_labels_conflict_keeps_column_and_priority(self):
        it = issue(5, ["статус: очередь", "статус: разработка"], status="Очередь", prio=3)
        conflicts = []
        fixes, clears, *_ = bo.plan([it], archive=False, conflicts=conflicts)
        self.assertEqual(fixes, [])
        self.assertEqual(clears, [])
        self.assertEqual(len(conflicts), 1)

    def test_priority_cleared_on_legal_leave(self):
        it = issue(6, ["статус: бизнес-анализ"], status="Очередь", prio=1)
        fixes, clears, *_ = bo.plan([it], archive=False)
        self.assertEqual([(f[1], f[2]) for f in fixes], [("Очередь", "Бизнес-анализ")])
        self.assertEqual(clears, [it])

    def test_priority_kept_when_open_done(self):
        it = issue(7, ["статус: готово"], status="Очередь", prio=2)
        fixes, clears, *_ = bo.plan([it], archive=False)
        self.assertEqual(fixes, [])
        self.assertEqual(clears, [])

    def test_hold_label_moves_to_hold(self):
        it = issue(8, ["статус: удержание"], status="Разработка")
        self.assertEqual(bo.wanted_status(it, []), "Удержание")


if __name__ == "__main__":
    unittest.main()
