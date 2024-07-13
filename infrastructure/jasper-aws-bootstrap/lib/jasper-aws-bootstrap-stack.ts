import * as cdk from 'aws-cdk-lib';
import { Construct } from 'constructs';
// import * as sqs from 'aws-cdk-lib/aws-sqs';
import * as s3 from 'aws-cdk-lib/aws-s3';
import * as ddb from 'aws-cdk-lib/aws-dynamodb';

export class JasperAwsBootstrapStack extends cdk.Stack {
  stateBucket: s3.Bucket;
  stateLockTable: ddb.Table;
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const namespace = this.stackName;
    const env = process.env.ENV_NAME || 'dev';

    let removalPolicy = cdk.RemovalPolicy.DESTROY;
    let s3BucketAutoDel: boolean = true;
    if (env == 'prod') {
      removalPolicy = cdk.RemovalPolicy.RETAIN;
      s3BucketAutoDel = false;
    }

    const kmsCmkey = new cdk.aws_kms.Key(this, `${namespace}-state-KMSKey`, {
      description: `KMS CMK for ${namespace} state`,
      enableKeyRotation: true,
      alias: `${namespace}-cmk-state-key`,
      removalPolicy: removalPolicy,
    });
    cdk.Tags.of(kmsCmkey).add('Name', `${namespace}-state-KMSKey`);
    const kmsCmkState = kmsCmkey.addAlias(namespace + "-state-alias-key")


    // create s3 bucket for state
    const s3BucketName = `${namespace}-state`;
    this.stateBucket = new s3.Bucket(this, s3BucketName, {
      versioned: true,
      removalPolicy: removalPolicy,
      bucketName: s3BucketName,
      autoDeleteObjects: s3BucketAutoDel,
      encryption: s3.BucketEncryption.KMS,
      encryptionKey: kmsCmkState,
      objectOwnership: s3.ObjectOwnership.BUCKET_OWNER_ENFORCED,
      blockPublicAccess: s3.BlockPublicAccess.BLOCK_ALL,
    });
    
    // create dynamodb table for state lock
    const stateLockTableName = `${namespace}-state-table`;
    this.stateLockTable = new ddb.Table(this, stateLockTableName, {
      tableName: stateLockTableName,
      partitionKey: { name: 'LockID', type: ddb.AttributeType.STRING },
      removalPolicy: cdk.RemovalPolicy.RETAIN_ON_UPDATE_OR_DELETE,
      billingMode: ddb.BillingMode.PAY_PER_REQUEST,
      encryption: ddb.TableEncryption.CUSTOMER_MANAGED,
      encryptionKey:kmsCmkey,
    });

    cdk.Tags.of(this.stateLockTable).add('Name', stateLockTableName);
  }
}
