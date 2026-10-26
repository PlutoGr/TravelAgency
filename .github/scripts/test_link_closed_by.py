"""Проверка выбора смерженного PR для чипа closedBy. Сеть не нужна."""

import unittest

import link_closed_by as link


def pr(number, head, title, body, merged_at, author="octocat"):
    return {
        "id": f"PR_{number}",
        "number": number,
        "head": head,
        "title": title,
        "body": body,
        "merged_at": merged_at,
        "author": author,
    }


class BranchAndText(unittest.TestCase):
    def test_branch_segment(self):
        self.assertTrue(link.branch_has_issue("fix/12-foo", 12))
        self.assertTrue(link.branch_has_issue("fix/12", 12))
        self.assertTrue(link.branch_has_issue("12", 12))
        self.assertTrue(link.branch_has_issue("feature/120-bar", 120))
        self.assertTrue(link.branch_has_issue("cursor/fix-198-slug-3f52", 198))

    def test_branch_does_not_match_longer_or_hex_suffix(self):
        self.assertFalse(link.branch_has_issue("fix/120-foo", 12))
        self.assertFalse(link.branch_has_issue("fix/12-foo", 1))
        self.assertFalse(link.branch_has_issue("fix/12-foo", 2))
        self.assertFalse(link.branch_has_issue("cursor/foo-3f52", 3))
        self.assertFalse(link.branch_has_issue("cursor/foo-3f52", 52))
        self.assertFalse(link.branch_has_issue("prefix12suffix", 12))
        self.assertFalse(link.branch_has_issue("", 12))

    def test_hash_ref_bounds(self):
        self.assertTrue(link.text_has_issue_ref("## Задача\n#12\n", 12))
        self.assertTrue(link.text_has_issue_ref("исправление(ci): пример (#60)", 60))
        self.assertTrue(link.text_has_issue_ref("см. #60.", 60))
        self.assertFalse(link.text_has_issue_ref("см. #120", 12))
        self.assertFalse(link.text_has_issue_ref("см. #12", 1))
        self.assertFalse(link.text_has_issue_ref("", 12))
        self.assertFalse(link.text_has_issue_ref(None, 12))


class PickBest(unittest.TestCase):
    def test_branch_beats_later_mention(self):
        older = pr(1, "fix/12-slug", "заголовок", "текст", "2020-01-01T00:00:00Z")
        newer = pr(2, "cursor/foo-3f52", "заголовок", "упоминание #12", "2026-01-01T00:00:00Z")
        self.assertEqual(link.pick_best([newer, older], 12)["number"], 1)

    def test_title_beats_body_only(self):
        body_only = pr(3, "cursor/a-3f52", "без номера", "текст #12", "2026-06-01T00:00:00Z")
        title = pr(4, "cursor/b-3f52", "задача #12", "без номера", "2026-01-01T00:00:00Z")
        self.assertEqual(link.pick_best([body_only, title], 12)["number"], 4)

    def test_later_merge_breaks_tie(self):
        early = pr(1, "fix/12-a", "t", "b", "2026-01-01T00:00:00Z")
        late = pr(2, "fix/12-b", "t", "b", "2026-06-01T00:00:00Z")
        self.assertEqual(link.pick_best([early, late], 12)["number"], 2)

    def test_higher_number_breaks_same_timestamp(self):
        low = pr(4, "fix/12-a", "t", "b", "2026-01-01T00:00:00Z")
        high = pr(5, "fix/12-b", "t", "b", "2026-01-01T00:00:00Z")
        self.assertEqual(link.pick_best([low, high], 12)["number"], 5)

    def test_dependabot_skipped(self):
        bot = pr(
            9,
            "dependabot/npm/leftpad",
            "bump",
            "см. #12",
            "2026-08-01T00:00:00Z",
            author="dependabot[bot]",
        )
        human = pr(3, "cursor/other-3f52", "t", "см. #12", "2026-01-01T00:00:00Z")
        self.assertEqual(link.pick_best([bot, human], 12)["number"], 3)
        self.assertIsNone(link.pick_best([bot], 12))

    def test_dependabot_author_even_on_normal_branch(self):
        bot = pr(9, "fix/12-slug", "t #12", "b", "2026-08-01T00:00:00Z", author="dependabot[bot]")
        self.assertIsNone(link.pick_best([bot], 12))

    def test_unmerged_and_empty(self):
        open_pr = pr(1, "fix/12-slug", "#12", "b", "")
        self.assertIsNone(link.pick_best([open_pr], 12))
        self.assertIsNone(link.pick_best([], 12))

    def test_closing_keyword_not_required(self):
        mentioned = pr(7, "cursor/no-number-3f52", "правка", "Задача: #44", "2026-02-02T00:00:00Z")
        chosen = link.pick_best([mentioned], 44)
        self.assertEqual(chosen["number"], 7)
        self.assertNotIn("closes", mentioned["body"].lower())


if __name__ == "__main__":
    unittest.main()
