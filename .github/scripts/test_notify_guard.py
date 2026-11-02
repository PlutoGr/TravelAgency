"""Проверка агрегирования webhook: один сигнал на голову PR и payload деплоя."""

import json
import subprocess
import unittest
from pathlib import Path

import notify_guard as guard


def run_check(name, status, conclusion, completed_at, suite, run_id, sha="abc"):
    return {
        "name": name,
        "status": status,
        "conclusion": conclusion,
        "completed_at": completed_at,
        "started_at": completed_at,
        "id": run_id,
        "head_sha": sha,
        "check_suite": {"id": suite},
    }


class RequiredNames(unittest.TestCase):
    def test_defaults_and_extra_once(self):
        names = guard.required_names(["gitleaks", "lint"])
        self.assertEqual(names[:4], list(guard.DEFAULT_REQUIRED))
        self.assertEqual(names[-1], "lint")
        self.assertEqual(len(names), 5)


class PrDecision(unittest.TestCase):
    def setUp(self):
        self.names = guard.DEFAULT_REQUIRED
        self.sha = "a" * 40

    def _all(self, suite_for_last=7, conclusion="success", sha=None):
        sha = sha or self.sha
        runs = []
        for index, name in enumerate(self.names):
            suite = suite_for_last if index == len(self.names) - 1 else 3
            runs.append(
                run_check(
                    name,
                    "completed",
                    conclusion if index == len(self.names) - 1 else "success",
                    f"2026-11-02T10:00:0{index}Z",
                    suite,
                    100 + index,
                    sha,
                )
            )
        return runs

    def test_incomplete_skips(self):
        runs = self._all()
        runs[-1]["status"] = "in_progress"
        runs[-1]["conclusion"] = None
        decision = guard.decide_pr(runs, [], self.sha, self.sha, 7)
        self.assertEqual(decision["action"], "skip")
        self.assertEqual(decision["reason"], "incomplete")

    def test_missing_name_skips(self):
        runs = self._all()[:-1]
        decision = guard.decide_pr(runs, [], self.sha, self.sha, 3)
        self.assertEqual(decision["reason"], "incomplete")

    def test_workflow_run_check_suite_id_sends_once(self):
        """«CI» и «Secret scan» это разные suite одного workflow_run. Шлёт только более поздний."""
        runs = self._all()
        earlier = guard.decide_pr(runs, [], self.sha, self.sha, 3)
        later = guard.decide_pr(runs, [], self.sha, self.sha, 7)
        self.assertEqual(earlier["reason"], "not-latest-suite")
        self.assertEqual(later["action"], "send")
        self.assertEqual(later["result"], "green")

    def test_green_only_from_latest_suite(self):
        runs = self._all()
        self.assertEqual(guard.decide_pr(runs, [], self.sha, self.sha, 3)["reason"], "not-latest-suite")
        sent = guard.decide_pr(runs, [], self.sha, self.sha, 7)
        self.assertEqual(sent["action"], "send")
        self.assertEqual(sent["result"], "green")
        self.assertEqual(sent["failed_checks"], [])

    def test_red_lists_failed_name(self):
        runs = self._all(conclusion="failure")
        sent = guard.decide_pr(runs, [], self.sha, self.sha, 7)
        self.assertEqual(sent["result"], "red")
        self.assertEqual(sent["failed_checks"], ["gitleaks"])

    def test_superseded_sha_skips(self):
        runs = self._all()
        decision = guard.decide_pr(runs, [], self.sha, "b" * 40, 7)
        self.assertEqual(decision["reason"], "superseded")

    def test_tie_uses_higher_check_id(self):
        runs = self._all()
        runs.append(
            run_check("gitleaks", "completed", "success", "2026-11-02T10:00:03Z", 9, 50, self.sha)
        )
        # id 103 из _all позже id 50, та же метка времени, что у gitleaks в _all (индекс 3, 10:00:03)
        self.assertEqual(guard.decide_pr(runs, [], self.sha, self.sha, 9)["reason"], "not-latest-suite")
        self.assertEqual(guard.decide_pr(runs, [], self.sha, self.sha, 7)["action"], "send")

    def test_rerun_newer_suite_sends_once(self):
        runs = self._all()
        runs.append(
            run_check("gitleaks", "completed", "success", "2026-11-02T11:00:00Z", 11, 200, self.sha)
        )
        self.assertEqual(guard.decide_pr(runs, [], self.sha, self.sha, 7)["reason"], "not-latest-suite")
        sent = guard.decide_pr(runs, [], self.sha, self.sha, 11)
        self.assertEqual(sent["action"], "send")
        self.assertEqual(sent["result"], "green")

    def test_in_progress_rerun_blocks_old_success(self):
        runs = self._all()
        runs.append(
            run_check("gitleaks", "in_progress", None, "2026-11-02T12:00:00Z", 12, 300, self.sha)
        )
        decision = guard.decide_pr(runs, [], self.sha, self.sha, 7)
        self.assertEqual(decision["reason"], "incomplete")

    def test_extra_required_from_rules_must_finish(self):
        runs = self._all()
        decision = guard.decide_pr(runs, ["lint"], self.sha, self.sha, 7)
        self.assertEqual(decision["reason"], "incomplete")
        runs.append(run_check("lint", "completed", "success", "2026-11-02T10:00:09Z", 8, 400, self.sha))
        self.assertEqual(guard.decide_pr(runs, ["lint"], self.sha, self.sha, 7)["reason"], "not-latest-suite")
        self.assertEqual(guard.decide_pr(runs, ["lint"], self.sha, self.sha, 8)["result"], "green")

    def test_optional_pending_check_does_not_block(self):
        runs = self._all()
        runs.append(run_check("Bugbot", "in_progress", None, "2026-11-02T12:00:00Z", 99, 1, self.sha))
        sent = guard.decide_pr(runs, [], self.sha, self.sha, 7)
        self.assertEqual(sent["result"], "green")

    def test_other_sha_on_a_check_is_ignored(self):
        runs = self._all()
        runs.append(
            run_check("gitleaks", "in_progress", None, "2026-11-02T13:00:00Z", 1, 9, "c" * 40)
        )
        sent = guard.decide_pr(runs, [], self.sha, self.sha, 7)
        self.assertEqual(sent["result"], "green")


class Payload(unittest.TestCase):
    def test_pr_payload_shape(self):
        decision = {"result": "red", "failed_checks": ["gitleaks"]}
        payload = guard.pr_payload(decision, "12", "a" * 40, "dependabot/npm/left-pad", "dependabot[bot]", "https://example/pull/12")
        self.assertEqual(payload["event"], "pr-checks")
        self.assertEqual(payload["pr"], 12)
        self.assertEqual(payload["author"], "dependabot[bot]")
        self.assertEqual(payload["failed_checks"], ["gitleaks"])
        self.assertEqual(payload["source"], "github-actions")

    def test_deploy_success_and_cancelled(self):
        ok = guard.deploy_payload("success", "b" * 40, "c" * 40, "main", 99, "https://example/run/99")
        self.assertEqual(ok["result"], "green")
        self.assertEqual(ok["deployed_sha"], "c" * 40)
        self.assertEqual(ok["conclusion"], "success")
        self.assertEqual(ok["run_id"], "99")
        bad = guard.deploy_payload("cancelled", "b" * 40, "c" * 40, "main", 100, "https://example/run/100")
        self.assertEqual(bad["result"], "red")
        self.assertEqual(bad["deployed_sha"], "")
        self.assertEqual(bad["conclusion"], "cancelled")

    def test_deploy_rejects_short_sha(self):
        payload = guard.deploy_payload("success", "b" * 40, "not-a-sha", "main", 1, "https://example")
        self.assertEqual(payload["deployed_sha"], "")

    def test_cli_pr_skip(self):
        raw = json.dumps(
            {
                "checks": [],
                "extra": [],
                "sha": "a" * 40,
                "pr_head": "b" * 40,
                "suite_id": 1,
                "pr": 1,
                "branch": "x",
                "author": "octocat",
                "html_url": "https://example",
            }
        )
        script = str(Path(__file__).with_name("notify_guard.py"))
        proc = subprocess.run(
            ["python3", script, "pr"],
            input=raw,
            text=True,
            capture_output=True,
            check=False,
        )
        self.assertEqual(proc.returncode, 0, proc.stderr)
        self.assertEqual(json.loads(proc.stdout)["reason"], "superseded")


if __name__ == "__main__":
    unittest.main()
