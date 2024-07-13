import * as cdk from 'aws-cdk-lib';
import { Template,Match } from 'aws-cdk-lib/assertions';
import * as JasperAwsBootstrap from '../lib/jasper-aws-bootstrap-stack';

// example test. To run these tests, uncomment this file along with the
// example resource in lib/jasper-aws-bootstrap-stack.ts
test('State S3 bucket created', () => {
    const app = new cdk.App();
    // WHEN
    const stack = new JasperAwsBootstrap.JasperAwsBootstrapStack(app, 'jasper-bootstrap-dev', {
      env: {
        account: '123456789012',
        region: 'ca-central-1'
      }
  
    });
    // THEN
  
    const template = Template.fromStack(stack);
  
    template.resourceCountIs('AWS::S3::Bucket', 1);
  
    
});

// test dynamodb table created
test('State Lock Table Created', () => {
    const app = new cdk.App();
    // WHEN
    const stack = new JasperAwsBootstrap.JasperAwsBootstrapStack(app, 'jasper-bootstrap-dev', {
      env: {
        account: '123456789012',
        region: 'ca-central-1'
      }
  
    });
    // THEN
  
    const template = Template.fromStack(stack);
  
    template.resourceCountIs('AWS::DynamoDB::Table', 1);
  
});
