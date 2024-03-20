uniffi::include_scaffolding!("cedarsharp");

use cedar_policy::*;

pub fn authorize(
    policy: &str,
    principal: &str,
    action: &str,
    resource: &str,
    context_json: &str,
    entities_json: &str)
        -> String {

    let context_parsed = Context::from_json_str(
        if context_json.is_empty() { r#"{}"# } else { context_json },
        None).unwrap();
    let entities_parsed = Entities::from_json_str(
        if entities_json.is_empty() { r#"[]"# } else { entities_json },
        None).expect("entity parse error");

    let policy_set: PolicySet = policy.parse().unwrap();
    let request = Request::new(
        Some(principal.parse().unwrap()), 
        Some(action.parse().unwrap()),
        Some(resource.parse().unwrap()),
        context_parsed,
        None).unwrap();

    let result = Authorizer::new().is_authorized(
        &request, 
        &policy_set, 
        &entities_parsed); 

    return if result.decision() == Decision::Allow { 
        "ALLOW".to_owned()
    } else {
        "DENY".to_owned()
    };
}

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

        assert_eq!(result, "ALLOW");
    }

    #[test]
    fn can_deny() {
        let policy: &str = r#"permit(principal == User::"alice", action == Action::"view", resource == File::"93");"#;
        let principal = r#"User::"bob""#;
        let action = r#"Action::"view""#;
        let resource = r#"File::"93""#;

        let result = authorize(policy, principal, action, resource, "", "");

        assert_eq!(result, "DENY");
    }
}
