uniffi::include_scaffolding!("cedarsharp");

extern crate stopwatch;

use std::str::FromStr;
use stopwatch::{Stopwatch};
use cedar_policy::*;

pub struct CedarPolicy {
    id: String,
    policy: String,
}

pub struct CedarResult {
    result: Decision,
    error: String
}

pub fn authorize(
    policies: Vec<CedarPolicy>,
    principal: &str,
    action: &str,
    resource: &str,
    context: &str,
    entities: &str)
        -> CedarResult {
    let parsed_policies: Vec<Policy> = policies.into_iter().map(|p| {
        let policy = p.policy.as_str();
        let parse_result = Policy::parse(Some(p.id), policy);
        parse_result.unwrap()
    }).collect();

    let p = principal.parse();
    let a = action.parse();
    let r = resource.parse();

    if (p.is_err() || a.is_err() || r.is_err()) {
        return CedarResult {
            result: Decision::Deny,
            error: "Failed to parse PAR.".to_owned()
        };
    }

    let policy_set: PolicySet = PolicySet::from_policies(parsed_policies).unwrap();
    let request = Request::new(
        Some(p.unwrap()),
        Some(a.unwrap()),
        Some(r.unwrap()),
        Context::from_json_str(
            if context.is_empty() { r#"{}"# } else { context },
            None).unwrap(),
        None).unwrap();

    let result = Authorizer::new().is_authorized(
        &request, 
        &policy_set, 
        &Entities::from_json_str(
            if entities.is_empty() { r#"[]"# } else { entities },
            None).expect("entity parse error")); 

    return CedarResult {
        result: result.decision(),
        error: "".to_owned()
    };
}

//struct AuthorizationResult {
//    decision: Decision,
//    policies: Vec<String>
//}
//
//impl AuthorizationResult {
//    fn new(decision: Decision, policies: Vec<String>) -> Self {
//        AuthorizationResult {
//            decision: decision,
//            policies: policies
//        }
//    }
//
//    fn decision(&self) -> Decision {
//        self.decision().to_owned()
//    }
//
//    fn policies(&self) -> Vec<String> {
//        self.policies().to_owned()
//    }
//}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn can_allow() {
        let policy: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from(r#"permit(principal == User::"alice", action == Action::"view", resource == File::"93");"#)
        };
        let principal = r#"User::"alice""#;
        let action = r#"Action::"view""#;
        let resource = r#"File::"93""#;

        let result = authorize(vec![policy], principal, action, resource, "", "");

        assert_eq!(result.result, Decision::Allow);
    }

    #[test]
    fn can_deny() {
        let policy: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from(r#"permit(principal == User::"alice", action == Action::"view", resource == File::"93");"#)
        };
        let principal = r#"User::"bob""#;
        let action = r#"Action::"view""#;
        let resource = r#"File::"93""#;

        let result = authorize(vec![policy], principal, action, resource, "", "");

        assert_eq!(result.result, Decision::Deny);
    }

    #[test]
    fn can_allow_with_multiple() {
        let policy_one: CedarPolicy = CedarPolicy {
            id: String::from("23"),
            policy: String::from(r#"permit(principal == User::"alice", action == Action::"view", resource == File::"93");"#),
        };
        let policy_two: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from(r#"permit(principal == User::"alice", action == Action::"view", resource == File::"95");"#)
        };
        let principal = r#"User::"alice""#;
        let action = r#"Action::"view""#;
        let resource = r#"File::"93""#;

        let result = authorize(vec![policy_one, policy_two], principal, action, resource, "", "");

        assert_eq!(result.result, Decision::Allow);
    }

    #[test]
    fn can_allow_with_context() {
        let policy: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from(r#"permit( principal in User::"Bob", action in [Action::"update", Action::"delete"], resource == Photo::"peppers.jpg") when { context.mfa_authenticated == true && context.request_client_ip == "42.42.42.42" };"#)
        };
        let principal: &str = r#"User::"Bob""#;
        let action: &str = r#"Action::"update""#;
        let resource: &str = r#"Photo::"peppers.jpg""#;
        let context: &str = r#"{"mfa_authenticated": true, "request_client_ip": "42.42.42.42", "oidc_scope": "profile" }"#;

        let result = authorize(vec![policy], principal, action, resource, context, "");

        assert_eq!(result.result, Decision::Allow);
    }

    #[test]
    fn can_deny_with_context() {
        let policy: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from(r#"permit( principal in User::"Bob", action in [Action::"update", Action::"delete"], resource == Photo::"peppers.jpg") when { context.mfa_authenticated == true && context.request_client_ip == "42.42.42.42" };"#)
        };
        let principal: &str = r#"User::"Bob""#;
        let action: &str = r#"Action::"update""#;
        let resource: &str = r#"Photo::"peppers.jpg""#;
        let context: &str = r#"{"mfa_authenticated": true, "request_client_ip": "23.23.23.23", "oidc_scope": "profile" }"#;

        let result = authorize(vec![policy], principal, action, resource, context, "");

        assert_eq!(result.result, Decision::Deny);
    }

    #[test]
    fn can_allow_role_with_entities() {
        let policy: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from(r#"permit(principal in Role::"photoJudges", action == Action::"view", resource == Photo::"peppers.jpg");"#)
        };
        let principal: &str = r#"User::"Bob""#;
        let action: &str = r#"Action::"view""#;
        let resource: &str = r#"Photo::"peppers.jpg""#;
        let entities: &str = r#"[ { "uid": { "type": "User", "id": "Bob" }, "attrs": {}, "parents": [ { "type": "Role", "id": "photoJudges" }, { "type": "Role", "id": "juniorPhotoJudges" } ] }, { "uid": { "type": "Role", "id": "photoJudges" }, "attrs": {}, "parents": [] }, { "uid": { "type": "Role", "id": "juniorPhotoJudges" }, "attrs": {}, "parents": [] } ]"#;

        let result = authorize(vec![policy], principal, action, resource, "", entities);

        assert_eq!(result.result, Decision::Allow);
    }

    #[test]
    fn can_deny_role_with_entities() {
        let policy: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from(r#"permit(principal in Role::"photoJudges", action == Action::"view", resource == Photo::"peppers.jpg");"#)
        };
        let principal: &str = r#"User::"Bob""#;
        let action: &str = r#"Action::"view""#;
        let resource: &str = r#"Photo::"peppers.jpg""#;
        let entities: &str = r#"[ { "uid": { "type": "User", "id": "Bob" }, "attrs": {}, "parents": [ { "type": "Role", "id": "photoSubmitters" }, { "type": "Role", "id": "juniorPhotoSubmitters" } ] }, { "uid": { "type": "Role", "id": "photoJudges" }, "attrs": {}, "parents": [] }, { "uid": { "type": "Role", "id": "juniorPhotoJudges" }, "attrs": {}, "parents": [] } ]"#;

        let result = authorize(vec![policy], principal, action, resource, "", entities);

        assert_eq!(result.result, Decision::Deny);
    }

    #[test]
    fn can_allow_avp() {
        let policy: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from("permit( principal in MinimalApi::User::\"39cc99d8-3cb7-4f7b-8ea3-af825fa20751\", action in [  MinimalApi::Action::\"ReadProject\",  MinimalApi::Action::\"UpdateProject\",  MinimalApi::Action::\"DeleteProject\",  MinimalApi::Action::\"CreateProjectUser\",  MinimalApi::Action::\"ReadProjectUser\",  MinimalApi::Action::\"UpdateProjectUser\",  MinimalApi::Action::\"DeleteProjectUser\",  MinimalApi::Action::\"CreateProjectData\",  MinimalApi::Action::\"ReadProjectData\",  MinimalApi::Action::\"UpdateProjectData\",  MinimalApi::Action::\"DeleteProjectData\",  MinimalApi::Action::\"CreateProjectResults\",  MinimalApi::Action::\"ReadProjectResults\",  MinimalApi::Action::\"UpdateProjectResults\",  MinimalApi::Action::\"DeleteProjectResults\" ], resource in MinimalApi::Project::\"9fec4852-59e5-4916-a6a8-233ac94f460c\");")
        };
        let principal = "MinimalApi::User::\"39cc99d8-3cb7-4f7b-8ea3-af825fa20751\"";
        let action = "MinimalApi::Action::\"ReadProject\"";
        let resource = "MinimalApi::Project::\"9fec4852-59e5-4916-a6a8-233ac94f460c\"";

        let result = authorize(vec![policy], principal, action, resource, "", "");

        assert_eq!(result.result, Decision::Allow);
    }

    #[test]
    fn can_allow_without_resource() {
        let policy: CedarPolicy = CedarPolicy {
            id: String::from("42"),
            policy: String::from(r#"permit (principal in MinimalApi::User::"39cc99d8-3cb7-4f7b-8ea3-af825fa20751", action in [MinimalApi::Action::"ExecuteTests"], resource);"#)
        };
        let principal = "MinimalApi::User::\"39cc99d8-3cb7-4f7b-8ea3-af825fa20751\"";
        let action = "MinimalApi::Action::\"ExecuteTests\"";
        let resource = "MinimalApi::PlaceHolder::\"0\"";

        let result = authorize(vec![policy], principal, action, resource, "", "");

        assert_eq!(result.result, Decision::Allow);
    }

    #[test]
    fn time_authz_calls() {
        let mut strategies: [&str; 3] = 
        [
            "CLAIMS",
            "AVP",
            "CEDAR"
        ];

        let mut principals: [&str; 3] =
        [
            "MinimalApi::User::\"user-one\"",
            "MinimalApi::User::\"user-two\"",
            "MinimalApi::User::\"user-three\""
        ];

        let mut actions: [&str; 8] =
        [
            "MinimalApi::Action::\"ReadProject\"",
            "MinimalApi::Action::\"CreateProject\"",
            "MinimalApi::Action::\"CreateProjectData\"",
            "MinimalApi::Action::\"CreateProjectResults\"",
            "MinimalApi::Action::\"DeleteProject\"",
            "MinimalApi::Action::\"DeleteProjectData\"",
            "MinimalApi::Action::\"DeleteProjectResults\"",
            "MinimalApi::Action::\"ReadProject\""
        ];

        let mut resources: [&str; 3] =
        [
            "MinimalApi::Project::\"project-one\"",
            "MinimalApi::Project::\"project-two\"",
            "MinimalApi::Project::\"project-three\""
        ];

        for i in 0..1000 {
            let sw = Stopwatch::start_new();
        }
    }

}
