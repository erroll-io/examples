uniffi::include_scaffolding!("cedarsharp");

use cedar_policy::*;

pub fn authorize(
    policy: &str,
    principal: &str,
    action: &str,
    resource: &str,
    context: &str,
    entities: &str)
        -> Decision {

    let policy_set: PolicySet = policy.parse().unwrap();
    let request = Request::new(
        Some(principal.parse().unwrap()), 
        Some(action.parse().unwrap()),
        Some(resource.parse().unwrap()),
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

    return result.decision();
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
        let policy: &str = r#"permit(principal == User::"alice", action == Action::"view", resource == File::"93");"#;
        let principal = r#"User::"alice""#;
        let action = r#"Action::"view""#;
        let resource = r#"File::"93""#;

        let result = authorize(policy, principal, action, resource, "", "");

        assert_eq!(result, Decision::Allow);
    }

    #[test]
    fn can_deny() {
        let policy: &str = r#"permit(principal == User::"alice", action == Action::"view", resource == File::"93");"#;
        let principal = r#"User::"bob""#;
        let action = r#"Action::"view""#;
        let resource = r#"File::"93""#;

        let result = authorize(policy, principal, action, resource, "", "");

        assert_eq!(result, Decision::Deny);
    }

    #[test]
    fn can_allow_with_context() {
        let policy: &str = r#"permit( principal in User::"Bob", action in [Action::"update", Action::"delete"], resource == Photo::"lena.jpg") when { context.mfa_authenticated == true && context.request_client_ip == "42.42.42.42" };"#;
        let principal: &str = r#"User::"Bob""#;
        let action: &str = r#"Action::"update""#;
        let resource: &str = r#"Photo::"lena.jpg""#;
        let context: &str = r#"{"mfa_authenticated": true, "request_client_ip": "42.42.42.42", "oidc_scope": "profile" }"#;

        let result = authorize(policy, principal, action, resource, context, "");

        assert_eq!(result, Decision::Allow);
    }

    #[test]
    fn can_deny_with_context() {
        let policy: &str = r#"permit( principal in User::"Bob", action in [Action::"update", Action::"delete"], resource == Photo::"lena.jpg") when { context.mfa_authenticated == true && context.request_client_ip == "42.42.42.42" };"#;
        let principal: &str = r#"User::"Bob""#;
        let action: &str = r#"Action::"update""#;
        let resource: &str = r#"Photo::"lena.jpg""#;
        let context: &str = r#"{"mfa_authenticated": true, "request_client_ip": "23.23.23.23", "oidc_scope": "profile" }"#;

        let result = authorize(policy, principal, action, resource, context, "");

        assert_eq!(result, Decision::Deny);
    }

    #[test]
    fn can_allow_role_with_entities() {
        let policy: &str = r#"permit(principal in Role::"photoJudges", action == Action::"view", resource == Photo::"lena.jpg");"#;
        let principal: &str = r#"User::"Bob""#;
        let action: &str = r#"Action::"view""#;
        let resource: &str = r#"Photo::"lena.jpg""#;
        let entities: &str = r#"[ { "uid": { "type": "User", "id": "Bob" }, "attrs": {}, "parents": [ { "type": "Role", "id": "photoJudges" }, { "type": "Role", "id": "juniorPhotoJudges" } ] }, { "uid": { "type": "Role", "id": "photoJudges" }, "attrs": {}, "parents": [] }, { "uid": { "type": "Role", "id": "juniorPhotoJudges" }, "attrs": {}, "parents": [] } ]"#;

        let result = authorize(policy, principal, action, resource, "", entities);

        assert_eq!(result, Decision::Allow);
    }
}
